using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using ReactCore.Backend.Data;
using ReactCore.Backend.Exceptions;
using ReactCore.Backend.Models.Dto;
using ReactCore.Backend.Options;
using ReactCore.Backend.Services;
using Xunit;

namespace ReactCore.Tests.Unit;

public sealed class ExternalApiServiceTests
{
    private static AppDbContext CreateDb()
    {
        var opts = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"ExternalApiServiceTests_{Guid.NewGuid():N}")
            .Options;

        return new AppDbContext(opts);
    }

    private static IOptions<ExternalApiOptions> CreateOptions(int rateLimitPerHour = 100, int cacheTtlMinutes = 30)
        => Options.Create(new ExternalApiOptions
        {
            RateLimitPerHour = rateLimitPerHour,
            CacheTtlMinutes = cacheTtlMinutes
        });

    [Fact]
    public async Task GetWeatherAsync_ReturnsCachedValue_WhenCacheHit()
    {
        await using var db = CreateDb();

        var cache = new Mock<ICacheService>();
        var weatherApi = new Mock<IWeatherApiService>();
        var rateLimiting = new Mock<IExternalApiRateLimitingService>();
        var logger = new Mock<ILogger<ExternalApiService>>();

        var cached = new WeatherDto(
            Temperature: 18,
            Condition: "Clear",
            Location: "Santo Domingo",
            Humidity: 60,
            WindSpeed: 3,
            FetchedAt: DateTimeOffset.UtcNow.AddMinutes(-10),
            IsCached: false,
            CacheExpiresAt: DateTimeOffset.UtcNow.AddMinutes(20));

        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(20);
        cache.Setup(c => c.TryGetWeatherAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((cached, expiresAt));

        var svc = new ExternalApiService(
            cache.Object,
            weatherApi.Object,
            rateLimiting.Object,
            db,
            CreateOptions(),
            logger.Object);

        var result = await svc.GetWeatherAsync("Santo Domingo", userId: null);

        result.IsCached.Should().BeTrue();
        result.CacheExpiresAt.Should().Be(expiresAt);

        rateLimiting.Verify(x => x.TryConsumeAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        weatherApi.Verify(x => x.FetchWeatherAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);

        db.ExternalApiCalls.Should().HaveCount(1);
        db.ExternalApiCalls.Single().WasCached.Should().BeTrue();
    }

    [Fact]
    public async Task GetWeatherAsync_FetchesAndCaches_WhenCacheMiss()
    {
        await using var db = CreateDb();

        var cache = new Mock<ICacheService>();
        var weatherApi = new Mock<IWeatherApiService>();
        var rateLimiting = new Mock<IExternalApiRateLimitingService>();
        var logger = new Mock<ILogger<ExternalApiService>>();

        cache.Setup(c => c.TryGetWeatherAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(((WeatherDto Value, DateTimeOffset ExpiresAt)?)null);

        rateLimiting.Setup(r => r.TryConsumeAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var live = new WeatherDto(
            Temperature: 21,
            Condition: "Clouds",
            Location: "Santo Domingo",
            Humidity: 55,
            WindSpeed: 4,
            FetchedAt: DateTimeOffset.UtcNow,
            IsCached: false,
            CacheExpiresAt: DateTimeOffset.UtcNow);

        weatherApi.Setup(w => w.FetchWeatherAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(live);

        WeatherDto? cachedValue = null;
        cache.Setup(c => c.SetWeatherAsync(It.IsAny<string>(), It.IsAny<WeatherDto>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .Callback<string, WeatherDto, DateTimeOffset, CancellationToken>((_, dto, _, _) => cachedValue = dto)
            .Returns(Task.CompletedTask);

        var svc = new ExternalApiService(
            cache.Object,
            weatherApi.Object,
            rateLimiting.Object,
            db,
            CreateOptions(rateLimitPerHour: 100, cacheTtlMinutes: 30),
            logger.Object);

        var result = await svc.GetWeatherAsync("Santo Domingo", userId: null);

        result.IsCached.Should().BeFalse();
        cachedValue.Should().NotBeNull();
        cachedValue!.IsCached.Should().BeFalse();

        db.ExternalApiCalls.Should().HaveCount(1);
        db.ExternalApiCalls.Single().WasCached.Should().BeFalse();
    }

    [Fact]
    public async Task GetWeatherAsync_Throws_WhenRateLimitExceeded_AndNoStaleCache()
    {
        await using var db = CreateDb();

        var cache = new Mock<ICacheService>();
        var weatherApi = new Mock<IWeatherApiService>();
        var rateLimiting = new Mock<IExternalApiRateLimitingService>();
        var logger = new Mock<ILogger<ExternalApiService>>();

        cache.Setup(c => c.TryGetWeatherAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(((WeatherDto Value, DateTimeOffset ExpiresAt)?)null);

        rateLimiting.Setup(r => r.TryConsumeAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        cache.Setup(c => c.TryGetStaleWeatherAsync(It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((WeatherDto?)null);

        var svc = new ExternalApiService(
            cache.Object,
            weatherApi.Object,
            rateLimiting.Object,
            db,
            CreateOptions(rateLimitPerHour: 1),
            logger.Object);

        var act = async () => await svc.GetWeatherAsync("Santo Domingo", userId: null);
        await act.Should().ThrowAsync<RateLimitExceededException>();

        db.ExternalApiCalls.Should().HaveCount(1);
        db.ExternalApiCalls.Single().StatusCode.Should().Be(503);
    }

    [Fact]
    public async Task GetWeatherAsync_ReturnsStale_WhenRateLimitExceeded_ButStaleExists()
    {
        await using var db = CreateDb();

        var cache = new Mock<ICacheService>();
        var weatherApi = new Mock<IWeatherApiService>();
        var rateLimiting = new Mock<IExternalApiRateLimitingService>();
        var logger = new Mock<ILogger<ExternalApiService>>();

        cache.Setup(c => c.TryGetWeatherAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(((WeatherDto Value, DateTimeOffset ExpiresAt)?)null);

        rateLimiting.Setup(r => r.TryConsumeAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var stale = new WeatherDto(
            Temperature: 16,
            Condition: "Rain",
            Location: "Santo Domingo",
            Humidity: 70,
            WindSpeed: 6,
            FetchedAt: DateTimeOffset.UtcNow.AddHours(-2),
            IsCached: false,
            CacheExpiresAt: DateTimeOffset.UtcNow);

        cache.Setup(c => c.TryGetStaleWeatherAsync(It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(stale);

        var svc = new ExternalApiService(
            cache.Object,
            weatherApi.Object,
            rateLimiting.Object,
            db,
            CreateOptions(rateLimitPerHour: 1),
            logger.Object);

        var result = await svc.GetWeatherAsync("Santo Domingo", userId: null);

        result.IsCached.Should().BeTrue();
        db.ExternalApiCalls.Should().HaveCount(1);
        db.ExternalApiCalls.Single().WasCached.Should().BeTrue();
    }

    [Fact]
    public async Task GetWeatherAsync_ReturnsStale_WhenApiUnavailable_AndStaleExists()
    {
        await using var db = CreateDb();

        var cache = new Mock<ICacheService>();
        var weatherApi = new Mock<IWeatherApiService>();
        var rateLimiting = new Mock<IExternalApiRateLimitingService>();
        var logger = new Mock<ILogger<ExternalApiService>>();

        cache.Setup(c => c.TryGetWeatherAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(((WeatherDto Value, DateTimeOffset ExpiresAt)?)null);

        rateLimiting.Setup(r => r.TryConsumeAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        weatherApi.Setup(w => w.FetchWeatherAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ApiUnavailableException("nope"));

        var stale = new WeatherDto(
            Temperature: 16,
            Condition: "Rain",
            Location: "Santo Domingo",
            Humidity: 70,
            WindSpeed: 6,
            FetchedAt: DateTimeOffset.UtcNow.AddHours(-2),
            IsCached: false,
            CacheExpiresAt: DateTimeOffset.UtcNow);

        cache.Setup(c => c.TryGetStaleWeatherAsync(It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(stale);

        var svc = new ExternalApiService(
            cache.Object,
            weatherApi.Object,
            rateLimiting.Object,
            db,
            CreateOptions(),
            logger.Object);

        var result = await svc.GetWeatherAsync("Santo Domingo", userId: null);

        result.IsCached.Should().BeTrue();
        db.ExternalApiCalls.Should().HaveCount(1);
        db.ExternalApiCalls.Single().WasCached.Should().BeTrue();
    }

    [Fact]
    public async Task GetWeatherAsync_RethrowsApiUnavailable_WhenNoStaleExists()
    {
        await using var db = CreateDb();

        var cache = new Mock<ICacheService>();
        var weatherApi = new Mock<IWeatherApiService>();
        var rateLimiting = new Mock<IExternalApiRateLimitingService>();
        var logger = new Mock<ILogger<ExternalApiService>>();

        cache.Setup(c => c.TryGetWeatherAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(((WeatherDto Value, DateTimeOffset ExpiresAt)?)null);

        rateLimiting.Setup(r => r.TryConsumeAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        weatherApi.Setup(w => w.FetchWeatherAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ApiUnavailableException("nope"));

        cache.Setup(c => c.TryGetStaleWeatherAsync(It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((WeatherDto?)null);

        var svc = new ExternalApiService(
            cache.Object,
            weatherApi.Object,
            rateLimiting.Object,
            db,
            CreateOptions(),
            logger.Object);

        var act = async () => await svc.GetWeatherAsync("Santo Domingo", userId: null);
        await act.Should().ThrowAsync<ApiUnavailableException>();

        db.ExternalApiCalls.Should().HaveCount(1);
        db.ExternalApiCalls.Single().StatusCode.Should().Be(503);
    }

    [Fact]
    public async Task GetWeatherAsync_RethrowsArgumentException_FromWeatherApi()
    {
        await using var db = CreateDb();

        var cache = new Mock<ICacheService>();
        var weatherApi = new Mock<IWeatherApiService>();
        var rateLimiting = new Mock<IExternalApiRateLimitingService>();
        var logger = new Mock<ILogger<ExternalApiService>>();

        cache.Setup(c => c.TryGetWeatherAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(((WeatherDto Value, DateTimeOffset ExpiresAt)?)null);

        rateLimiting.Setup(r => r.TryConsumeAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        weatherApi.Setup(w => w.FetchWeatherAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentException("bad location", "location"));

        var svc = new ExternalApiService(
            cache.Object,
            weatherApi.Object,
            rateLimiting.Object,
            db,
            CreateOptions(),
            logger.Object);

        var act = async () => await svc.GetWeatherAsync("Santo Domingo", userId: null);
        await act.Should().ThrowAsync<ArgumentException>();

        db.ExternalApiCalls.Should().HaveCount(1);
        db.ExternalApiCalls.Single().StatusCode.Should().Be(400);
    }
}

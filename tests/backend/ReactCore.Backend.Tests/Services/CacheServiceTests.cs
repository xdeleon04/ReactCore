using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using ReactCore.Backend.Data;
using ReactCore.Backend.Models.Dto;
using ReactCore.Backend.Services;
using Xunit;

namespace ReactCore.Tests.Unit;

public sealed class CacheServiceTests
{
    private static AppDbContext CreateDb()
    {
        var opts = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"CacheServiceTests_{Guid.NewGuid():N}")
            .Options;

        return new AppDbContext(opts);
    }

    private static IMemoryCache CreateMemoryCache()
        => new MemoryCache(new MemoryCacheOptions { SizeLimit = 1024 });

    [Fact]
    public async Task SetThenTryGetWeatherAsync_ReturnsValue_AndIncrementsHitCounter()
    {
        await using var db = CreateDb();
        using var memoryCache = CreateMemoryCache();

        var logger = new Mock<ILogger<CacheService>>();
        var svc = new CacheService(memoryCache, db, logger.Object);

        var key = "weather_santo domingo";
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(10);
        var dto = new WeatherDto(
            Temperature: 25,
            Condition: "Clear",
            Location: "Santo Domingo",
            Humidity: 55,
            WindSpeed: 4,
            FetchedAt: DateTimeOffset.UtcNow,
            IsCached: false,
            CacheExpiresAt: expiresAt);

        await svc.SetWeatherAsync(key, dto, expiresAt);

        var result = await svc.TryGetWeatherAsync(key);

        result.Should().NotBeNull();
        result!.Value.Value.Temperature.Should().Be(25);
        result.Value.ExpiresAt.Should().Be(expiresAt);

        // The persisted cache row should have a hit.
        var entry = await db.CacheEntries.SingleAsync();
        entry.Hits.Should().Be(1);
    }

    [Fact]
    public async Task TryGetWeatherAsync_ReturnsNull_WhenNotInMemory()
    {
        await using var db = CreateDb();
        using var memoryCache = CreateMemoryCache();

        var logger = new Mock<ILogger<CacheService>>();
        var svc = new CacheService(memoryCache, db, logger.Object);

        var result = await svc.TryGetWeatherAsync("missing");
        result.Should().BeNull();

        db.CacheEntries.Should().BeEmpty();
    }

    [Fact]
    public async Task TryGetStaleWeatherAsync_ReturnsNull_WhenEntryDoesNotExist()
    {
        await using var db = CreateDb();
        using var memoryCache = CreateMemoryCache();

        var logger = new Mock<ILogger<CacheService>>();
        var svc = new CacheService(memoryCache, db, logger.Object);

        var result = await svc.TryGetStaleWeatherAsync("missing", TimeSpan.FromHours(1));
        result.Should().BeNull();
    }

    [Fact]
    public async Task TryGetStaleWeatherAsync_ReturnsValue_WhenEntryIsRecentAndValidJson()
    {
        await using var db = CreateDb();
        using var memoryCache = CreateMemoryCache();

        var logger = new Mock<ILogger<CacheService>>();
        var svc = new CacheService(memoryCache, db, logger.Object);

        var key = "weather_santo domingo";
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(10);
        var dto = new WeatherDto(
            Temperature: 22,
            Condition: "Clouds",
            Location: "Santo Domingo",
            Humidity: 60,
            WindSpeed: 5,
            FetchedAt: DateTimeOffset.UtcNow.AddMinutes(-1),
            IsCached: false,
            CacheExpiresAt: expiresAt);

        await svc.SetWeatherAsync(key, dto, expiresAt);

        // Simulate app restart by clearing memory cache, forcing persisted fallback path.
        memoryCache.Remove(key);

        var result = await svc.TryGetStaleWeatherAsync(key, TimeSpan.FromHours(24));

        result.Should().NotBeNull();
        result!.Temperature.Should().Be(22);
        result.Condition.Should().Be("Clouds");
    }

    [Fact]
    public async Task TryGetStaleWeatherAsync_ReturnsNull_WhenJsonIsInvalid()
    {
        await using var db = CreateDb();
        using var memoryCache = CreateMemoryCache();

        var logger = new Mock<ILogger<CacheService>>();
        var svc = new CacheService(memoryCache, db, logger.Object);

        var key = "weather_santo domingo";
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(10);
        var dto = new WeatherDto(
            Temperature: 22,
            Condition: "Clouds",
            Location: "Santo Domingo",
            Humidity: 60,
            WindSpeed: 5,
            FetchedAt: DateTimeOffset.UtcNow.AddMinutes(-1),
            IsCached: false,
            CacheExpiresAt: expiresAt);

        await svc.SetWeatherAsync(key, dto, expiresAt);

        var entry = await db.CacheEntries.SingleAsync();
        entry.DataJson = "{ not-json }";
        await db.SaveChangesAsync();

        memoryCache.Remove(key);

        var result = await svc.TryGetStaleWeatherAsync(key, TimeSpan.FromHours(24));
        result.Should().BeNull();
    }

    [Fact]
    public async Task RemoveAsync_RemovesFromMemoryAndDatabase()
    {
        await using var db = CreateDb();
        using var memoryCache = CreateMemoryCache();

        var logger = new Mock<ILogger<CacheService>>();
        var svc = new CacheService(memoryCache, db, logger.Object);

        var key = "weather_santo domingo";
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(10);
        var dto = new WeatherDto(
            Temperature: 22,
            Condition: "Clouds",
            Location: "Santo Domingo",
            Humidity: 60,
            WindSpeed: 5,
            FetchedAt: DateTimeOffset.UtcNow,
            IsCached: false,
            CacheExpiresAt: expiresAt);

        await svc.SetWeatherAsync(key, dto, expiresAt);

        (await db.CacheEntries.CountAsync()).Should().Be(1);

        await svc.RemoveAsync(key);

        (await db.CacheEntries.CountAsync()).Should().Be(0);
        (await svc.TryGetWeatherAsync(key)).Should().BeNull();
    }
}

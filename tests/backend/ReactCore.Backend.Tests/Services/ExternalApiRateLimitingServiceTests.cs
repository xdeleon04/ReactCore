using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using ReactCore.Backend.Data;
using ReactCore.Backend.Models;
using ReactCore.Backend.Services;
using Xunit;

namespace ReactCore.Tests.Unit;

public sealed class ExternalApiRateLimitingServiceTests
{
    private static AppDbContext CreateDb()
    {
        var opts = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"ExternalApiRateLimitingServiceTests_{Guid.NewGuid():N}")
            .Options;

        return new AppDbContext(opts);
    }

    [Fact]
    public async Task TryConsumeAsync_CreatesUsageRowAndIncrementsCount()
    {
        await using var db = CreateDb();

        var logger = new Mock<ILogger<ExternalApiRateLimitingService>>();
        var svc = new ExternalApiRateLimitingService(db, logger.Object);

        var allowed1 = await svc.TryConsumeAsync("openweathermap", hourlyLimit: 5);
        var allowed2 = await svc.TryConsumeAsync("openweathermap", hourlyLimit: 5);

        allowed1.Should().BeTrue();
        allowed2.Should().BeTrue();

        var usage = await db.ApiQuotaUsages.SingleAsync();
        usage.CallCount.Should().Be(2);
        usage.QuotaLimit.Should().Be(5);
    }

    [Fact]
    public async Task TryConsumeAsync_ReturnsFalse_WhenLimitReached()
    {
        await using var db = CreateDb();

        var logger = new Mock<ILogger<ExternalApiRateLimitingService>>();
        var svc = new ExternalApiRateLimitingService(db, logger.Object);

        (await svc.TryConsumeAsync("openweathermap", hourlyLimit: 1)).Should().BeTrue();
        (await svc.TryConsumeAsync("openweathermap", hourlyLimit: 1)).Should().BeFalse();

        var usage = await db.ApiQuotaUsages.SingleAsync();
        usage.CallCount.Should().Be(1);
    }

    [Fact]
    public async Task TryConsumeAsync_LogsWarning_WhenUsageIsAtOrAboveNinetyPercent()
    {
        await using var db = CreateDb();

        var logger = new Mock<ILogger<ExternalApiRateLimitingService>>();
        var svc = new ExternalApiRateLimitingService(db, logger.Object);

        // With limit 10, reaching 9 calls is 90%.
        for (var i = 0; i < 9; i++)
        {
            (await svc.TryConsumeAsync("openweathermap", hourlyLimit: 10)).Should().BeTrue();
        }

        logger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("quota usage at", StringComparison.OrdinalIgnoreCase)),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task GetQuotaStatusAsync_ComputesCountsAndAlertsAndSuccessRate()
    {
        await using var db = CreateDb();

        var apiName = "openweathermap";
        var now = DateTimeOffset.UtcNow;
        var hourStart = new DateTimeOffset(now.Year, now.Month, now.Day, now.Hour, 0, 0, TimeSpan.Zero);

        db.ApiQuotaUsages.Add(new ApiQuotaUsage
        {
            ApiName = apiName,
            PeriodStart = hourStart,
            CallCount = 9,
            QuotaLimit = 10,
            ResetsAt = hourStart.AddHours(1)
        });

        // 10 calls today, 8 successes.
        for (var i = 0; i < 8; i++)
        {
            db.ExternalApiCalls.Add(new ExternalApiCall
            {
                ApiName = apiName,
                Endpoint = "/weather",
                Timestamp = now.AddMinutes(-i),
                StatusCode = 200,
                ResponseTimeMs = 10,
                WasCached = false,
                Error = null,
                UserId = null
            });
        }

        for (var i = 0; i < 2; i++)
        {
            db.ExternalApiCalls.Add(new ExternalApiCall
            {
                ApiName = apiName,
                Endpoint = "/weather",
                Timestamp = now.AddMinutes(-(i + 20)),
                StatusCode = 503,
                ResponseTimeMs = 10,
                WasCached = false,
                Error = "nope",
                UserId = null
            });
        }

        await db.SaveChangesAsync();

        var logger = new Mock<ILogger<ExternalApiRateLimitingService>>();
        var svc = new ExternalApiRateLimitingService(db, logger.Object);

        var status = await svc.GetQuotaStatusAsync(apiName, hourlyLimit: 10);

        status.ApiName.Should().Be(apiName);
        status.CallsThisHour.Should().Be(9);
        status.CallsToday.Should().Be(10);
        status.QuotaUsed.Should().Be(9);
        status.QuotaRemaining.Should().Be(1);
        status.QuotaPercentage.Should().Be(90);
        status.Alerts.Should().NotBeEmpty();
        status.SuccessRate.Should().Be(80.0);
        status.LastCall.Should().NotBeNull();
    }
}

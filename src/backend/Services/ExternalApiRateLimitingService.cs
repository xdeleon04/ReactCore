using Microsoft.EntityFrameworkCore;
using ReactCore.Backend.Data;
using ReactCore.Backend.Models;
using ReactCore.Backend.Models.Dto;

namespace ReactCore.Backend.Services;

/// <summary>
/// Tracks hourly external API quota usage and provides quota status for admin monitoring.
/// </summary>
public sealed class ExternalApiRateLimitingService : IExternalApiRateLimitingService
{
    private readonly AppDbContext _db;
    private readonly ILogger<ExternalApiRateLimitingService> _logger;

    public ExternalApiRateLimitingService(AppDbContext db, ILogger<ExternalApiRateLimitingService> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// Attempts to consume one unit of quota for the current hour.
    /// Returns false if the hourly limit has been reached.
    /// </summary>
    public async Task<bool> TryConsumeAsync(string apiName, int hourlyLimit, CancellationToken cancellationToken = default)
    {
        hourlyLimit = Math.Max(1, hourlyLimit);

        var periodStart = GetCurrentHourStartUtc();
        var resetsAt = periodStart.AddHours(1);

        const int maxRetries = 3;
        for (var attempt = 0; attempt < maxRetries; attempt++)
        {
            var usage = await _db.ApiQuotaUsages
                .FirstOrDefaultAsync(x => x.ApiName == apiName && x.PeriodStart == periodStart, cancellationToken);

            if (usage is null)
            {
                usage = new ApiQuotaUsage
                {
                    ApiName = apiName,
                    PeriodStart = periodStart,
                    QuotaLimit = hourlyLimit,
                    CallCount = 0,
                    ResetsAt = resetsAt
                };

                _db.ApiQuotaUsages.Add(usage);
            }

            usage.QuotaLimit = hourlyLimit;
            usage.ResetsAt = resetsAt;

            if (usage.CallCount >= hourlyLimit)
            {
                return false;
            }

            usage.CallCount += 1;

            try
            {
                await _db.SaveChangesAsync(cancellationToken);

                var percent = (int)Math.Round((double)usage.CallCount * 100 / hourlyLimit);
                if (percent >= 90)
                {
                    _logger.LogWarning("External API quota usage at {Percent}% for {ApiName} ({CallCount}/{Limit})", percent, apiName, usage.CallCount, hourlyLimit);
                }

                return true;
            }
            catch (DbUpdateConcurrencyException) when (attempt < maxRetries - 1)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(50), cancellationToken);
            }
        }

        return false;
    }

    /// <summary>
    /// Returns quota usage and success rate information for admin monitoring.
    /// </summary>
    public async Task<ApiQuotaStatusDto> GetQuotaStatusAsync(string apiName, int hourlyLimit, CancellationToken cancellationToken = default)
    {
        hourlyLimit = Math.Max(1, hourlyLimit);

        var now = DateTimeOffset.UtcNow;
        var hourStart = GetCurrentHourStartUtc();
        var dayStart = new DateTimeOffset(now.UtcDateTime.Date, TimeSpan.Zero);

        var callsThisHour = await _db.ApiQuotaUsages
            .AsNoTracking()
            .Where(x => x.ApiName == apiName && x.PeriodStart == hourStart)
            .Select(x => x.CallCount)
            .FirstOrDefaultAsync(cancellationToken);

        var callsToday = await _db.ExternalApiCalls
            .AsNoTracking()
            .Where(x => x.ApiName == apiName && x.Timestamp >= dayStart)
            .CountAsync(cancellationToken);

        var totalToday = await _db.ExternalApiCalls
            .AsNoTracking()
            .Where(x => x.ApiName == apiName && x.Timestamp >= dayStart)
            .CountAsync(cancellationToken);

        var successToday = await _db.ExternalApiCalls
            .AsNoTracking()
            .Where(x => x.ApiName == apiName && x.Timestamp >= dayStart && x.StatusCode >= 200 && x.StatusCode < 300)
            .CountAsync(cancellationToken);

        var lastCall = await _db.ExternalApiCalls
            .AsNoTracking()
            .Where(x => x.ApiName == apiName)
            .OrderByDescending(x => x.Timestamp)
            .Select(x => (DateTimeOffset?)x.Timestamp)
            .FirstOrDefaultAsync(cancellationToken);

        var quotaUsed = callsThisHour;
        var quotaRemaining = Math.Max(0, hourlyLimit - quotaUsed);
        var quotaPercentage = (int)Math.Round((double)quotaUsed * 100 / hourlyLimit);

        var alerts = new List<ApiQuotaAlertDto>();
        if (quotaPercentage >= 90)
        {
            alerts.Add(new ApiQuotaAlertDto(
                Severity: "warning",
                Message: $"Approaching 90% quota usage (currently at {quotaPercentage}%)"));
        }

        var successRate = totalToday == 0 ? 100d : (double)successToday * 100d / totalToday;

        return new ApiQuotaStatusDto(
            ApiName: apiName,
            CallsToday: callsToday,
            CallsThisHour: callsThisHour,
            QuotaLimit: hourlyLimit,
            QuotaUsed: quotaUsed,
            QuotaRemaining: quotaRemaining,
            QuotaPercentage: quotaPercentage,
            ResetsAt: hourStart.AddHours(1),
            SuccessRate: Math.Round(successRate, 1),
            LastCall: lastCall,
            Alerts: alerts);
    }

    private static DateTimeOffset GetCurrentHourStartUtc()
    {
        var now = DateTimeOffset.UtcNow;
        return new DateTimeOffset(now.Year, now.Month, now.Day, now.Hour, 0, 0, TimeSpan.Zero);
    }
}

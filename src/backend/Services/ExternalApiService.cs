using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ReactCore.Backend.Data;
using ReactCore.Backend.Exceptions;
using ReactCore.Backend.Models;
using ReactCore.Backend.Models.Dto;
using ReactCore.Backend.Options;

namespace ReactCore.Backend.Services;

/// <summary>
/// Coordinates cache, rate limiting, and live fetch for external weather data.
/// Persists an audit record for every request.
/// </summary>
public sealed class ExternalApiService : IExternalApiService
{
    private const string ApiName = "openweathermap";
    private static readonly TimeSpan MaxStaleAge = TimeSpan.FromHours(24);

    private readonly ICacheService _cache;
    private readonly IWeatherApiService _weatherApi;
    private readonly IExternalApiRateLimitingService _rateLimiting;
    private readonly AppDbContext _db;
    private readonly ExternalApiOptions _options;
    private readonly ILogger<ExternalApiService> _logger;

    public ExternalApiService(
        ICacheService cache,
        IWeatherApiService weatherApi,
        IExternalApiRateLimitingService rateLimiting,
        AppDbContext db,
        IOptions<ExternalApiOptions> options,
        ILogger<ExternalApiService> logger)
    {
        _cache = cache;
        _weatherApi = weatherApi;
        _rateLimiting = rateLimiting;
        _db = db;
        _options = options.Value;
        _logger = logger;
    }

    /// <summary>
    /// Returns weather for a location, preferring cached data and enforcing hourly quota.
    /// May return stale cached data when the external API is unavailable.
    /// </summary>
    public async Task<WeatherDto> GetWeatherAsync(string location, Guid? userId, CancellationToken cancellationToken = default)
    {
        location = NormalizeLocation(location);
        var cacheKey = BuildCacheKey(location);

        var stopwatch = Stopwatch.StartNew();

        var cached = await _cache.TryGetWeatherAsync(cacheKey, cancellationToken);
        if (cached.HasValue)
        {
            var (dto, expiresAt) = cached.Value;

            if (dto.FetchedAt < DateTimeOffset.UtcNow.Subtract(MaxStaleAge))
            {
                await _cache.RemoveAsync(cacheKey, cancellationToken);
            }
            else
            {
                var response = dto with { IsCached = true, CacheExpiresAt = expiresAt };
                await RecordCallAsync(apiEndpoint: "/weather", statusCode: 200, wasCached: true, error: null, userId: userId, elapsedMs: stopwatch.ElapsedMilliseconds, cancellationToken);
                return response;
            }
        }

        var allowed = await _rateLimiting.TryConsumeAsync(ApiName, _options.RateLimitPerHour, cancellationToken);
        if (!allowed)
        {
            var stale = await _cache.TryGetStaleWeatherAsync(cacheKey, MaxStaleAge, cancellationToken);
            if (stale is not null)
            {
                var response = stale with { IsCached = true, CacheExpiresAt = DateTimeOffset.UtcNow };
                await RecordCallAsync(apiEndpoint: "/weather", statusCode: 200, wasCached: true, error: null, userId: userId, elapsedMs: stopwatch.ElapsedMilliseconds, cancellationToken);
                return response;
            }

            await RecordCallAsync(apiEndpoint: "/weather", statusCode: 503, wasCached: false, error: "Rate limit exceeded", userId: userId, elapsedMs: stopwatch.ElapsedMilliseconds, cancellationToken);
            throw new RateLimitExceededException("External API rate limit exceeded.");
        }

        try
        {
            var live = await _weatherApi.FetchWeatherAsync(location, cancellationToken);
            var expiresAt = DateTimeOffset.UtcNow.AddMinutes(Math.Max(1, _options.CacheTtlMinutes));

            var response = live with
            {
                IsCached = false,
                CacheExpiresAt = expiresAt
            };

            await _cache.SetWeatherAsync(cacheKey, response, expiresAt, cancellationToken);

            await RecordCallAsync(apiEndpoint: "/weather", statusCode: 200, wasCached: false, error: null, userId: userId, elapsedMs: stopwatch.ElapsedMilliseconds, cancellationToken);
            return response;
        }
        catch (ArgumentException ex)
        {
            await RecordCallAsync(apiEndpoint: "/weather", statusCode: 400, wasCached: false, error: ex.Message, userId: userId, elapsedMs: stopwatch.ElapsedMilliseconds, cancellationToken);
            throw;
        }
        catch (ApiUnavailableException ex)
        {
            var stale = await _cache.TryGetStaleWeatherAsync(cacheKey, MaxStaleAge, cancellationToken);
            if (stale is not null)
            {
                var response = stale with { IsCached = true, CacheExpiresAt = DateTimeOffset.UtcNow };
                await RecordCallAsync(apiEndpoint: "/weather", statusCode: 200, wasCached: true, error: null, userId: userId, elapsedMs: stopwatch.ElapsedMilliseconds, cancellationToken);
                return response;
            }

            _logger.LogWarning(ex, "External weather API unavailable for {Location}", location);
            await RecordCallAsync(apiEndpoint: "/weather", statusCode: 503, wasCached: false, error: ex.Message, userId: userId, elapsedMs: stopwatch.ElapsedMilliseconds, cancellationToken);
            throw;
        }
    }

    private static string NormalizeLocation(string location)
    {
        if (string.IsNullOrWhiteSpace(location))
        {
            return "Santo Domingo";
        }

        var trimmed = location.Trim();
        if (trimmed.Length > 100)
        {
            throw new ArgumentException("location must be <= 100 characters", nameof(location));
        }

        return trimmed;
    }

    private static string BuildCacheKey(string location)
        => $"weather_{location.Trim().ToLowerInvariant()}";

    private async Task RecordCallAsync(
        string apiEndpoint,
        int statusCode,
        bool wasCached,
        string? error,
        Guid? userId,
        long elapsedMs,
        CancellationToken cancellationToken)
    {
        try
        {
            _db.ExternalApiCalls.Add(new ExternalApiCall
            {
                ApiName = ApiName,
                Endpoint = apiEndpoint,
                Timestamp = DateTimeOffset.UtcNow,
                StatusCode = statusCode,
                ResponseTimeMs = elapsedMs,
                WasCached = wasCached,
                Error = error,
                UserId = userId
            });

            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to persist ExternalApiCall audit row");
        }
    }
}

using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using ReactCore.Backend.Data;
using ReactCore.Backend.Models;
using ReactCore.Backend.Models.Dto;

namespace ReactCore.Backend.Services;

/// <summary>
/// Provides a two-tier cache for external API responses (in-memory fast path + persisted cache entries).
/// </summary>
public sealed class CacheService : ICacheService
{
    private const string ApiName = "openweathermap";
    private const string DataTypeWeather = "weather";

    private readonly IMemoryCache _cache;
    private readonly AppDbContext _db;
    private readonly ILogger<CacheService> _logger;

    public CacheService(IMemoryCache cache, AppDbContext db, ILogger<CacheService> logger)
    {
        _cache = cache;
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// Attempts to read a fresh cached weather value.
    /// Returns null when the in-memory cache does not contain the key.
    /// </summary>
    public async Task<(WeatherDto Value, DateTimeOffset ExpiresAt)?> TryGetWeatherAsync(
        string cacheKey,
        CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetValue<CacheEnvelope<WeatherDto>>(cacheKey, out var envelope) && envelope is not null)
        {
            await TryIncrementCacheHitAsync(cacheKey, cancellationToken);
            return (envelope.Value, envelope.ExpiresAt);
        }

        return null;
    }

    /// <summary>
    /// Attempts to read a persisted cached weather value within a maximum staleness window.
    /// </summary>
    public async Task<WeatherDto?> TryGetStaleWeatherAsync(
        string cacheKey,
        TimeSpan maxAge,
        CancellationToken cancellationToken = default)
    {
        var cutoff = DateTimeOffset.UtcNow.Subtract(maxAge);

        var entry = await _db.CacheEntries
            .AsNoTracking()
            .Where(e => e.ApiName == ApiName && e.DataType == DataTypeWeather && e.CacheKey == cacheKey)
            .OrderByDescending(e => e.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (entry is null)
        {
            return null;
        }

        if (entry.CreatedAt < cutoff)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(entry.DataJson))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<WeatherDto>(entry.DataJson);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to deserialize cached weather data for {CacheKey}", cacheKey);
            return null;
        }
    }

    /// <summary>
    /// Stores a weather value in memory and persists it for stale fallback.
    /// </summary>
    public async Task SetWeatherAsync(
        string cacheKey,
        WeatherDto value,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var absoluteTtl = expiresAt > now ? expiresAt - now : TimeSpan.FromSeconds(1);
        var slidingTtl = TimeSpan.FromMinutes(Math.Min(15, Math.Max(1, absoluteTtl.TotalMinutes)));

        var envelope = new CacheEnvelope<WeatherDto>(value, expiresAt);
        _cache.Set(cacheKey, envelope, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = absoluteTtl,
            SlidingExpiration = slidingTtl,
            Size = 1
        });

        var json = JsonSerializer.Serialize(value);

        var existing = await _db.CacheEntries
            .FirstOrDefaultAsync(e => e.ApiName == ApiName && e.DataType == DataTypeWeather && e.CacheKey == cacheKey, cancellationToken);

        if (existing is null)
        {
            _db.CacheEntries.Add(new CacheEntry
            {
                ApiName = ApiName,
                DataType = DataTypeWeather,
                CacheKey = cacheKey,
                CreatedAt = value.FetchedAt,
                ExpiresAt = expiresAt,
                Hits = 0,
                DataJson = json
            });
        }
        else
        {
            existing.CreatedAt = value.FetchedAt;
            existing.ExpiresAt = expiresAt;
            existing.DataJson = json;
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Removes the cache key from memory and from persisted entries.
    /// </summary>
    public async Task RemoveAsync(string cacheKey, CancellationToken cancellationToken = default)
    {
        _cache.Remove(cacheKey);

        var entries = await _db.CacheEntries
            .Where(e => e.ApiName == ApiName && e.DataType == DataTypeWeather && e.CacheKey == cacheKey)
            .ToListAsync(cancellationToken);

        if (entries.Count > 0)
        {
            _db.CacheEntries.RemoveRange(entries);
            await _db.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task TryIncrementCacheHitAsync(string cacheKey, CancellationToken cancellationToken)
    {
        try
        {
            var entry = await _db.CacheEntries
                .FirstOrDefaultAsync(e => e.ApiName == ApiName && e.DataType == DataTypeWeather && e.CacheKey == cacheKey, cancellationToken);

            if (entry is null)
            {
                return;
            }

            entry.Hits += 1;
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Cache hit increment failed for {CacheKey}", cacheKey);
        }
    }

    private sealed record CacheEnvelope<T>(T Value, DateTimeOffset ExpiresAt);
}

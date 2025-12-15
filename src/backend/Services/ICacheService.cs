using ReactCore.Backend.Models.Dto;

namespace ReactCore.Backend.Services;

public interface ICacheService
{
    Task<(WeatherDto Value, DateTimeOffset ExpiresAt)?> TryGetWeatherAsync(string cacheKey, CancellationToken cancellationToken = default);
    Task<WeatherDto?> TryGetStaleWeatherAsync(string cacheKey, TimeSpan maxAge, CancellationToken cancellationToken = default);
    Task SetWeatherAsync(string cacheKey, WeatherDto value, DateTimeOffset expiresAt, CancellationToken cancellationToken = default);
    Task RemoveAsync(string cacheKey, CancellationToken cancellationToken = default);
}

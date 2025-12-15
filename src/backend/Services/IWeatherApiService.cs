using ReactCore.Backend.Models.Dto;

namespace ReactCore.Backend.Services;

public interface IWeatherApiService
{
    Task<WeatherDto> FetchWeatherAsync(string location, CancellationToken cancellationToken = default);
}

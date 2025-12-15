using ReactCore.Backend.Models.Dto;

namespace ReactCore.Backend.Services;

public interface IExternalApiService
{
    Task<WeatherDto> GetWeatherAsync(string location, Guid? userId, CancellationToken cancellationToken = default);
}

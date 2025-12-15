using ReactCore.Backend.Models.Dto;

namespace ReactCore.Backend.Services;

public interface IExternalApiRateLimitingService
{
    Task<bool> TryConsumeAsync(string apiName, int hourlyLimit, CancellationToken cancellationToken = default);
    Task<ApiQuotaStatusDto> GetQuotaStatusAsync(string apiName, int hourlyLimit, CancellationToken cancellationToken = default);
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using ReactCore.Backend.Options;
using ReactCore.Backend.Services;

namespace ReactCore.Backend.Controllers.Admin;

/// <summary>
/// Admin-only endpoints for monitoring external API quota usage.
/// </summary>
[ApiController]
[Route("api/admin/api-usage")]
[Authorize(Roles = "admin")]
[EnableRateLimiting("admin")]
public class AdminApiUsageController : ControllerBase
{
    private const string ApiName = "openweathermap";

    private readonly IExternalApiRateLimitingService _rateLimiting;
    private readonly ExternalApiOptions _options;

    public AdminApiUsageController(IExternalApiRateLimitingService rateLimiting, IOptions<ExternalApiOptions> options)
    {
        _rateLimiting = rateLimiting;
        _options = options.Value;
    }

    /// <summary>
    /// Returns current quota usage status for the configured external API.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken = default)
    {
        var dto = await _rateLimiting.GetQuotaStatusAsync(ApiName, _options.RateLimitPerHour, cancellationToken);
        return Ok(dto);
    }
}

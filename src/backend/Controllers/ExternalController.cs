using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReactCore.Backend.Exceptions;
using ReactCore.Backend.Services;

namespace ReactCore.Backend.Controllers;

/// <summary>
/// Authenticated endpoints for consuming backend-proxied external data.
/// </summary>
[ApiController]
[Route("api/external")]
[Authorize]
public class ExternalController : ControllerBase
{
    private readonly IExternalApiService _externalApi;

    public ExternalController(IExternalApiService externalApi)
    {
        _externalApi = externalApi;
    }

    /// <summary>
    /// Gets current weather for a location (defaults to London).
    /// </summary>
    [HttpGet("weather")]
    public async Task<IActionResult> GetWeather(
        [FromQuery] string? location,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = GetUserIdOrNull();
            var dto = await _externalApi.GetWeatherAsync(location ?? "Santo Domingo", userId, cancellationToken);
            return Ok(dto);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new
            {
                statusCode = 400,
                message = "Invalid query parameters",
                errors = new Dictionary<string, string[]> { { ex.ParamName ?? "location", [ex.Message] } }
            });
        }
        catch (RateLimitExceededException)
        {
            return StatusCode(503, new { statusCode = 503, message = "Weather service temporarily unavailable." });
        }
        catch (ApiUnavailableException)
        {
            return StatusCode(503, new { statusCode = 503, message = "Weather service unavailable." });
        }
    }

    private Guid? GetUserIdOrNull()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        return userIdClaim is not null && Guid.TryParse(userIdClaim.Value, out var id) ? id : null;
    }
}

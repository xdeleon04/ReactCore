using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using ReactCore.Backend.Services.Admin;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace ReactCore.Backend.Controllers.Admin;

[ApiController]
[Route("api/admin/users")]
[Authorize(Roles = "admin")]
[EnableRateLimiting("admin")]
public class AdminUsersController : ControllerBase
{
    private readonly IAdminUserService _adminUserService;

    public AdminUsersController(IAdminUserService adminUserService)
    {
        _adminUserService = adminUserService;
    }

    [HttpGet]
    public async Task<IActionResult> ListUsers(
        [FromQuery] string? email,
        [FromQuery] string? role,
        [FromQuery] bool? isActive,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 20,
        CancellationToken cancellationToken = default)
    {
        var (items, total) = await _adminUserService.ListUsersAsync(email, role, isActive, skip, take, cancellationToken);
        return Ok(new { items, total, skip = Math.Max(0, skip), take = Math.Clamp(take, 1, 100) });
    }

    [HttpGet("{userId:guid}")]
    public async Task<IActionResult> GetUserDetail([FromRoute] Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _adminUserService.GetUserDetailAsync(userId, cancellationToken);
        if (user is null)
        {
            return NotFound(new { error = "User not found", statusCode = 404 });
        }
        return Ok(user);
    }

    [HttpPatch("{userId:guid}/deactivate")]
    public async Task<IActionResult> Deactivate([FromRoute] Guid userId, [FromBody] AdminDeactivateRequest request, CancellationToken cancellationToken = default)
    {
        var adminUserId = GetAdminUserId();
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();

        var result = await _adminUserService.DeactivateAsync(adminUserId, userId, request.Reason, ip, cancellationToken);
        if (!result.Success)
        {
            if (result.Error == "User not found")
            {
                return NotFound(new { error = result.Error, statusCode = 404 });
            }
            return BadRequest(new { error = result.Error, statusCode = 400 });
        }

        return Ok(new
        {
            success = true,
            userId,
            isActive = false,
            deactivatedAt = result.Timestamp,
        });
    }

    [HttpPatch("{userId:guid}/reactivate")]
    public async Task<IActionResult> Reactivate([FromRoute] Guid userId, CancellationToken cancellationToken = default)
    {
        var adminUserId = GetAdminUserId();
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();

        var result = await _adminUserService.ReactivateAsync(adminUserId, userId, ip, cancellationToken);
        if (!result.Success)
        {
            if (result.Error == "User not found")
            {
                return NotFound(new { error = result.Error, statusCode = 404 });
            }
            return BadRequest(new { error = result.Error, statusCode = 400 });
        }

        return Ok(new
        {
            success = true,
            userId,
            isActive = true,
            reactivatedAt = result.Timestamp,
        });
    }

    private Guid GetAdminUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim is null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            throw new UnauthorizedAccessException("Invalid or missing user id claim");
        }
        return userId;
    }
}

public class AdminDeactivateRequest
{
    [MaxLength(500)]
    public string? Reason { get; set; }
}

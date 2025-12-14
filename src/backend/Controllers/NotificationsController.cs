using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using ReactCore.Backend.Services;
using System.Security.Claims;

namespace ReactCore.Backend.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _notificationService;

    public NotificationsController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    /// <summary>
    /// Returns triggered restock notifications for the authenticated user and marks them as delivered.
    /// </summary>
    [HttpGet("pending")]
    [EnableRateLimiting("notifications")]
    public async Task<IActionResult> GetPending(CancellationToken cancellationToken = default)
    {
        var userId = GetUserId();
        var notifications = await _notificationService.CheckPendingNotificationsAsync(userId, cancellationToken);
        return Ok(notifications);
    }

    private Guid GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim is null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            throw new UnauthorizedAccessException("Invalid or missing user id claim");
        }

        return userId;
    }
}

using System.Security.Claims;
using System.Text.Json;
using ReactCore.Backend.Models;
using ReactCore.Backend.Models.Enums;
using ReactCore.Backend.Repositories;

namespace ReactCore.Backend.Middleware;

public class AdminAuthorizationMiddleware
{
    private readonly RequestDelegate _next;

    public AdminAuthorizationMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IAdminActionRepository adminActions, ILogger<AdminAuthorizationMiddleware> logger)
    {
        var path = context.Request.Path.Value;
        if (string.IsNullOrWhiteSpace(path) || !path.StartsWith("/api/admin", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        if (context.User?.Identity?.IsAuthenticated != true)
        {
            await _next(context);
            return;
        }

        if (context.User.IsInRole("admin"))
        {
            await _next(context);
            return;
        }

        var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var email = context.User.FindFirst(ClaimTypes.Email)?.Value;

        if (!string.IsNullOrWhiteSpace(userIdClaim) && Guid.TryParse(userIdClaim, out var userId))
        {
            try
            {
                var entityId = Truncate(path, 128);
                var ip = context.Connection.RemoteIpAddress?.ToString();

                await adminActions.AddAsync(new AdminAction
                {
                    AdminUserId = userId,
                    ActionType = AdminActionType.UnauthorizedAccessAttempt.ToString(),
                    EntityType = "AdminEndpoint",
                    EntityId = entityId,
                    IpAddress = ip,
                    Reason = "Access denied: admin role required",
                    OldValues = null,
                    NewValues = email is null ? null : JsonSerializer.Serialize(new { email }),
                    Timestamp = DateTime.UtcNow,
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to write unauthorized admin access audit entry");
            }
        }

        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        await context.Response.WriteAsJsonAsync(new { error = "Access denied: admin role required", statusCode = 403 }, context.RequestAborted);
    }

    private static string Truncate(string value, int maxLength)
    {
        if (value.Length <= maxLength) return value;
        return value.Substring(0, maxLength);
    }
}

using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using ReactCore.Backend.Data;

namespace ReactCore.Backend.Middleware;

public class IsActiveUserMiddleware
{
    private readonly RequestDelegate _next;

    public IsActiveUserMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, AppDbContext db)
    {
        if (context.User?.Identity?.IsAuthenticated != true)
        {
            await _next(context);
            return;
        }

        var path = context.Request.Path.Value;
        if (!string.IsNullOrEmpty(path) && path.StartsWith("/api/auth", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            await _next(context);
            return;
        }

        bool? isActive;
        try
        {
            isActive = await db.Users
                .AsNoTracking()
                .Where(u => u.Id == userId)
                .Select(u => (bool?)u.IsActive)
                .FirstOrDefaultAsync(context.RequestAborted);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            // Request was aborted by the client; don't turn that into a 500.
            return;
        }

        if (isActive == false)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { error = "Account is inactive", statusCode = 401 }, context.RequestAborted);
            return;
        }

        await _next(context);
    }
}

using System.Net;
using System.Text.Json;

namespace ReactCore.Backend.Middleware;

public class RateLimitMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RateLimitMiddleware> _logger;

    public RateLimitMiddleware(RequestDelegate next, ILogger<RateLimitMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Too many login attempts"))
        {
            const string errorMessage = "Too many login attempts. Please try again later.";
            _logger.LogWarning("Login rate limit exceeded");

            context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            context.Response.ContentType = "application/json";

            var response = new { message = errorMessage };
            await context.Response.WriteAsync(JsonSerializer.Serialize(response));
        }
    }
}

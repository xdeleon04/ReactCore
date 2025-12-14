using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ReactCore.Backend.Middleware;

public static class CorsMiddleware
{
    public const string PolicyName = "AllowFrontend";

    public static IServiceCollection AddFrontendCors(this IServiceCollection services, IConfiguration configuration)
    {
        var origin = configuration["Cors:FrontendOrigin"] ?? "http://localhost:5173";

        services.AddCors(options =>
        {
            options.AddPolicy(PolicyName,
                policy => policy.WithOrigins(origin)
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials());
        });

        return services;
    }

    public static IApplicationBuilder UseFrontendCors(this IApplicationBuilder app)
    {
        return app.UseCors(PolicyName);
    }
}

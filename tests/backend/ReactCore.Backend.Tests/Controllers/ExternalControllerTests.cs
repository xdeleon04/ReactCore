using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using Moq;
using ReactCore.Backend.Exceptions;
using ReactCore.Backend.Models.Dto;
using ReactCore.Backend.Services;
using ReactCore.Tests;

namespace ReactCore.Backend.Tests.Integration;

public sealed class ExternalControllerTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private const string TestJwtKey = "SuperSecretKeyForTesting12345!@#$%";
    private const string TestIssuer = "TestIssuer";
    private const string TestAudience = "TestAudience";

    private static string CreateJwt(string role)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestJwtKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new(ClaimTypes.Role, role)
        };

        var token = new JwtSecurityToken(
            issuer: TestIssuer,
            audience: TestAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(30),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    [Fact]
    public async Task GetWeather_WithValidToken_ReturnsOk()
    {
        var externalApi = new Mock<IExternalApiService>();
        externalApi.Setup(x => x.GetWeatherAsync(It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WeatherDto(
                Temperature: 12,
                Condition: "Clear",
                Location: "Santo Domingo",
                Humidity: 50,
                WindSpeed: 3,
                FetchedAt: DateTimeOffset.UtcNow,
                IsCached: false,
                CacheExpiresAt: DateTimeOffset.UtcNow.AddMinutes(30)));

        var factory = CreateFactoryWithExternalApi(externalApi.Object);
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", CreateJwt("user"));

        var response = await client.GetAsync("/api/external/weather?location=Santo Domingo");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<WeatherDto>();
        Assert.NotNull(body);
        Assert.Equal("Santo Domingo", body!.Location);
    }

    [Fact]
    public async Task GetWeather_WhenArgumentException_ReturnsBadRequest()
    {
        var externalApi = new Mock<IExternalApiService>();
        externalApi.Setup(x => x.GetWeatherAsync(It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentException("location must be <= 100 characters", "location"));

        var factory = CreateFactoryWithExternalApi(externalApi.Object);
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", CreateJwt("user"));

        var response = await client.GetAsync("/api/external/weather?location=ThisLocationNameIsDefinitelyLongerThanOneHundredCharacters_01234567890123456789012345678901234567890");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        Assert.Contains("Invalid query parameters", json);
        Assert.Contains("location", json);
    }

    [Fact]
    public async Task GetWeather_WhenApiUnavailable_Returns503()
    {
        var externalApi = new Mock<IExternalApiService>();
        externalApi.Setup(x => x.GetWeatherAsync(It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ApiUnavailableException("down"));

        var factory = CreateFactoryWithExternalApi(externalApi.Object);
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", CreateJwt("user"));

        var response = await client.GetAsync("/api/external/weather?location=Santo Domingo");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }

    [Fact]
    public async Task AdminApiUsage_WithNonAdminRole_ReturnsForbidden()
    {
        var rateLimiting = new Mock<IExternalApiRateLimitingService>();
        rateLimiting.Setup(x => x.GetQuotaStatusAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApiQuotaStatusDto(
                ApiName: "openweathermap",
                CallsToday: 1,
                CallsThisHour: 1,
                QuotaLimit: 100,
                QuotaUsed: 1,
                QuotaRemaining: 99,
                QuotaPercentage: 1,
                ResetsAt: DateTimeOffset.UtcNow.AddMinutes(30),
                SuccessRate: 100,
                LastCall: null,
                Alerts: Array.Empty<ApiQuotaAlertDto>()));

        var factory = CreateFactoryWithRateLimiting(rateLimiting.Object);
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", CreateJwt("user"));

        var response = await client.GetAsync("/api/admin/api-usage");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AdminApiUsage_WithAdminRole_ReturnsOk()
    {
        var rateLimiting = new Mock<IExternalApiRateLimitingService>();
        rateLimiting.Setup(x => x.GetQuotaStatusAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApiQuotaStatusDto(
                ApiName: "openweathermap",
                CallsToday: 2,
                CallsThisHour: 2,
                QuotaLimit: 100,
                QuotaUsed: 2,
                QuotaRemaining: 98,
                QuotaPercentage: 2,
                ResetsAt: DateTimeOffset.UtcNow.AddMinutes(30),
                SuccessRate: 100,
                LastCall: DateTimeOffset.UtcNow,
                Alerts: Array.Empty<ApiQuotaAlertDto>()));

        var factory = CreateFactoryWithRateLimiting(rateLimiting.Object);
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", CreateJwt("admin"));

        var response = await client.GetAsync("/api/admin/api-usage");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<ApiQuotaStatusDto>();
        Assert.NotNull(body);
        Assert.Equal("openweathermap", body!.ApiName);
    }

    private static WebApplicationFactory<Program> CreateFactoryWithExternalApi(IExternalApiService externalApi)
    {
        var baseFactory = new CustomWebApplicationFactory<Program>();
        return baseFactory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    {"Jwt:Key", TestJwtKey},
                    {"Jwt:Issuer", TestIssuer},
                    {"Jwt:Audience", TestAudience},
                    {"ExternalApis:OpenWeatherMap:RateLimitPerHour", "100"}
                });
            });

            builder.ConfigureServices(services =>
            {
                services.RemoveAll(typeof(IExternalApiService));
                services.AddSingleton(externalApi);
            });
        });
    }

    private static WebApplicationFactory<Program> CreateFactoryWithRateLimiting(IExternalApiRateLimitingService rateLimiting)
    {
        var baseFactory = new CustomWebApplicationFactory<Program>();
        return baseFactory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    {"Jwt:Key", TestJwtKey},
                    {"Jwt:Issuer", TestIssuer},
                    {"Jwt:Audience", TestAudience},
                    {"ExternalApis:OpenWeatherMap:RateLimitPerHour", "100"}
                });
            });

            builder.ConfigureServices(services =>
            {
                services.RemoveAll(typeof(IExternalApiRateLimitingService));
                services.AddSingleton(rateLimiting);
            });
        });
    }
}

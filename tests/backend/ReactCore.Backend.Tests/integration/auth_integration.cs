using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using ReactCore.Backend.Data;
using ReactCore.Backend.Models;
using ReactCore.Backend.Controllers;
using ReactCore.Tests;
using Xunit;

namespace ReactCore.Backend.Tests.Integration;

public class AuthIntegrationTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private const string TestJwtKey = "SuperSecretKeyForTesting12345!@#$%";
    private const string TestIssuer = "TestIssuer";
    private const string TestAudience = "TestAudience";

    public AuthIntegrationTests(CustomWebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((context, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    {"Jwt:Key", TestJwtKey},
                    {"Jwt:Issuer", TestIssuer},
                    {"Jwt:Audience", TestAudience}
                });
            });

            builder.ConfigureTestServices(services =>
            {
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));

                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                services.AddDbContext<AppDbContext>(options =>
                {
                    options.UseInMemoryDatabase("InMemoryDbForTesting");
                });
            });
        });
    }

    private string GenerateExpiredToken()
    {
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestJwtKey));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, "testuser"),
            new Claim(JwtRegisteredClaimNames.Email, "integration@test.com"),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: TestIssuer,
            audience: TestAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(-10), // Expired
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    [Fact]
    public async Task Login_FullFlow_ReturnsToken()
    {
        // Arrange
        var client = _factory.CreateClient();
        var email = "integration@test.com";
        var password = "Password123!";
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(password);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();

            db.Users.Add(new User
            {
                Email = email,
                PasswordHash = passwordHash,
                Role = "User",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        }

        var loginRequest = new { Email = email, Password = password };

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(result);
        Assert.False(string.IsNullOrEmpty(result.AccessToken));
        Assert.False(string.IsNullOrEmpty(result.RefreshToken));
    }

    [Fact]
    public async Task Login_InvalidCredentials_ReturnsUnauthorized()
    {
        // Arrange
        var client = _factory.CreateClient();
        var email = "integration_fail@test.com";
        var password = "Password123!";
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(password);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();

            db.Users.Add(new User
            {
                Email = email,
                PasswordHash = passwordHash,
                Role = "User",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        }

        var loginRequest = new { Email = email, Password = "WrongPassword!" };

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithExpiredToken_RefreshesAndSucceeds()
    {
        // Arrange
        var client = _factory.CreateClient();
        var email = "integration@test.com";
        var password = "Password123!";
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(password);

        // Seed user
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();

            db.Users.Add(new User
            {
                Email = email,
                PasswordHash = passwordHash,
                Role = "User",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        }

        // 1. Login to get a valid refresh token (and access token, which we'll ignore)
        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new { Email = email, Password = password });
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>();
        var refreshToken = loginResult!.RefreshToken;

        // 2. Try to access protected endpoint with EXPIRED access token
        var expiredToken = GenerateExpiredToken();
        var request1 = new HttpRequestMessage(HttpMethod.Get, "/api/user/profile");
        request1.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", expiredToken);

        var response1 = await client.SendAsync(request1);
        Assert.Equal(HttpStatusCode.Unauthorized, response1.StatusCode);

        // 3. Call Refresh Endpoint
        var refreshRequest = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh");
        // Assuming refresh token is passed in cookie as per spec
        refreshRequest.Headers.Add("Cookie", $"refreshToken={refreshToken}");

        var refreshResponse = await client.SendAsync(refreshRequest);
        Assert.Equal(HttpStatusCode.OK, refreshResponse.StatusCode);

        var refreshResult = await refreshResponse.Content.ReadFromJsonAsync<AuthResponse>();
        var newAccessToken = refreshResult!.AccessToken;
        Assert.False(string.IsNullOrEmpty(newAccessToken));

        // 4. Retry protected endpoint with NEW access token
        var request2 = new HttpRequestMessage(HttpMethod.Get, "/api/user/profile");
        request2.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", newAccessToken);

        var response2 = await client.SendAsync(request2);
        Assert.Equal(HttpStatusCode.OK, response2.StatusCode);
    }

    [Fact]
    public async Task Login_ShouldReturn429_WhenRateLimitExceeded()
    {
        // Arrange
        var client = _factory.CreateClient();
        var email = $"ratelimit_{Guid.NewGuid()}@example.com";

        // Create user
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Users.Add(new User
            {
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!")
            });
            await db.SaveChangesAsync();
        }

        var loginRequest = new LoginRequest { Email = email, Password = "WrongPassword!" };

        // Act & Assert - 5 failed attempts
        for (int i = 0; i < 5; i++)
        {
            var response = await client.PostAsJsonAsync("/api/auth/login", loginRequest);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        // 6th attempt should be 429
        var blockedResponse = await client.PostAsJsonAsync("/api/auth/login", loginRequest);
        Assert.Equal(HttpStatusCode.TooManyRequests, blockedResponse.StatusCode);
    }
}

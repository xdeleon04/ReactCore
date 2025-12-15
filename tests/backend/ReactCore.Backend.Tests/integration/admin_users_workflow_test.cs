using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using ReactCore.Backend.Data;
using ReactCore.Backend.Models;
using ReactCore.Tests;

namespace ReactCore.Backend.Tests.Integration;

public class AdminUsersWorkflowTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private const string TestJwtKey = "SuperSecretKeyForTesting12345!@#$%";
    private const string TestIssuer = "TestIssuer";
    private const string TestAudience = "TestAudience";

    private readonly WebApplicationFactory<Program> _factory;

    public AdminUsersWorkflowTests(CustomWebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    {"Jwt:Key", TestJwtKey},
                    {"Jwt:Issuer", TestIssuer},
                    {"Jwt:Audience", TestAudience}
                });
            });
        });
    }

    private static string GenerateToken(Guid userId, string email, string role)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestJwtKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Email, email),
            new Claim(ClaimTypes.Role, role),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        var token = new JwtSecurityToken(
            issuer: TestIssuer,
            audience: TestAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(15),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    [Fact]
    public async Task DeactivateUser_PreventsLogin_AndWritesAuditLog()
    {
        var adminUserId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        const string password = "P@ssw0rd123!";
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(password);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();

            db.Users.Add(new User { Id = adminUserId, Email = "admin@example.com", PasswordHash = "hash", Role = "admin", IsActive = true });
            db.Users.Add(new User { Id = userId, Email = "user@example.com", PasswordHash = passwordHash, Role = "user", IsActive = true });

            db.SaveChanges();
        }

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
            "Bearer",
            GenerateToken(adminUserId, "admin@example.com", "admin"));

        var deactivate = await client.PatchAsJsonAsync($"/api/admin/users/{userId}/deactivate", new { reason = "integration" });
        Assert.Equal(HttpStatusCode.OK, deactivate.StatusCode);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var updated = await db.Users.FindAsync(userId);
            Assert.NotNull(updated);
            Assert.False(updated.IsActive);

            Assert.Contains(db.AdminActions, a => a.ActionType == "UserDeactivate" && a.EntityType == "User" && a.EntityId == userId.ToString());
        }

        // Attempt login as deactivated user
        var loginResponse = await _factory.CreateClient().PostAsJsonAsync("/api/auth/login", new { email = "user@example.com", password });
        Assert.Equal(HttpStatusCode.Unauthorized, loginResponse.StatusCode);
    }

    [Fact]
    public async Task ReactivateUser_AllowsLogin_AndWritesAuditLog()
    {
        var adminUserId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        const string password = "P@ssw0rd123!";
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(password);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();

            db.Users.Add(new User { Id = adminUserId, Email = "admin@example.com", PasswordHash = "hash", Role = "admin", IsActive = true });
            db.Users.Add(new User { Id = userId, Email = "user@example.com", PasswordHash = passwordHash, Role = "user", IsActive = false });

            db.SaveChanges();
        }

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
            "Bearer",
            GenerateToken(adminUserId, "admin@example.com", "admin"));

        var reactivate = await client.PatchAsync($"/api/admin/users/{userId}/reactivate", content: null);
        Assert.Equal(HttpStatusCode.OK, reactivate.StatusCode);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var updated = await db.Users.FindAsync(userId);
            Assert.NotNull(updated);
            Assert.True(updated.IsActive);

            Assert.Contains(db.AdminActions, a => a.ActionType == "UserReactivate" && a.EntityType == "User" && a.EntityId == userId.ToString());
        }

        // Attempt login as reactivated user
        var loginResponse = await _factory.CreateClient().PostAsJsonAsync("/api/auth/login", new { email = "user@example.com", password });
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
    }
}

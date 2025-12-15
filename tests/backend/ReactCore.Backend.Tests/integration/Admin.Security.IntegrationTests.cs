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

public class AdminSecurityIntegrationTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private const string TestJwtKey = "SuperSecretKeyForTesting12345!@#$%";
    private const string TestIssuer = "TestIssuer";
    private const string TestAudience = "TestAudience";

    private readonly WebApplicationFactory<Program> _factory;

    public AdminSecurityIntegrationTests(CustomWebApplicationFactory<Program> factory)
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
    public async Task NonAdmin_CannotExecuteProtectedAdminLogic_AndAttemptIsAudited()
    {
        var actorUserId = Guid.NewGuid();
        var actorEmail = "user@example.com";
        var targetUserId = Guid.NewGuid();

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
            "Bearer",
            GenerateToken(actorUserId, actorEmail, "user"));

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();

            db.Users.AddRange(
                new User
                {
                    Id = actorUserId,
                    Email = actorEmail,
                    PasswordHash = "hash",
                    Role = "user",
                    IsActive = true,
                    IsLocked = false
                },
                new User
                {
                    Id = targetUserId,
                    Email = "target@example.com",
                    PasswordHash = "hash",
                    Role = "user",
                    IsActive = true,
                    IsLocked = false
                });

            db.SaveChanges();
        }

        var response = await client.PatchAsJsonAsync($"/api/admin/users/{targetUserId}/deactivate", new { reason = "test" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var target = await db.Users.FindAsync(targetUserId);
            Assert.NotNull(target);
            Assert.True(target.IsActive);

            var expectedEntityId = $"/api/admin/users/{targetUserId}/deactivate";
            var action = db.AdminActions.SingleOrDefault(a => a.AdminUserId == actorUserId && a.ActionType == "UnauthorizedAccessAttempt" && a.EntityId == expectedEntityId);
            Assert.NotNull(action);
        }
    }
}

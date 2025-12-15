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
using ReactCore.Backend.Models.Dto;
using ReactCore.Backend.Models;
using ReactCore.Tests;

namespace ReactCore.Backend.Tests.Contract;

public class AdminUsersContractTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private const string TestJwtKey = "SuperSecretKeyForTesting12345!@#$%";
    private const string TestIssuer = "TestIssuer";
    private const string TestAudience = "TestAudience";

    private readonly WebApplicationFactory<Program> _factory;

    public AdminUsersContractTests(CustomWebApplicationFactory<Program> factory)
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

    private HttpClient CreateAdminClient(Guid? adminUserId = null)
    {
        var client = _factory.CreateClient();

        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
            "Bearer",
            GenerateToken(adminUserId ?? Guid.NewGuid(), "admin@example.com", "admin"));

        return client;
    }

    [Fact]
    public async Task GetAdminUsers_WithoutToken_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/admin/users");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetAdminUsers_WithNonAdminRole_ReturnsForbidden()
    {
        var client = _factory.CreateClient();

        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
            "Bearer",
            GenerateToken(Guid.NewGuid(), "user@example.com", "user"));

        var response = await client.GetAsync("/api/admin/users");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetAdminUsers_WithAdminRole_ReturnsOk()
    {
        var client = _factory.CreateClient();

        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
            "Bearer",
            GenerateToken(Guid.NewGuid(), "admin@example.com", "admin"));

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();

            db.Users.Add(new User
            {
                Id = Guid.NewGuid(),
                Email = $"u_{Guid.NewGuid():N}@example.com",
                PasswordHash = "hash",
                Role = "user",
                IsActive = true,
                IsLocked = false
            });

            db.SaveChanges();
        }

        var response = await client.GetAsync("/api/admin/users");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetAdminUsers_WithEmailFilter_FiltersResults()
    {
        var adminUserId = Guid.NewGuid();
        var client = CreateAdminClient(adminUserId);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();

            db.Users.Add(new User { Id = adminUserId, Email = "admin@example.com", PasswordHash = "hash", Role = "admin", IsActive = true });

            db.Users.AddRange(
                new User { Id = Guid.NewGuid(), Email = "alice@example.com", PasswordHash = "hash", Role = "user", IsActive = true },
                new User { Id = Guid.NewGuid(), Email = "bob@example.com", PasswordHash = "hash", Role = "user", IsActive = true });

            db.SaveChanges();
        }

        var response = await client.GetAsync("/api/admin/users?email=ali&skip=0&take=20");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<PagedResult<UserListDto>>();
        Assert.NotNull(body);
        Assert.Single(body.Items);
        Assert.Equal("alice@example.com", body.Items[0].Email);
    }

    [Fact]
    public async Task GetAdminUserDetail_WithMissingUser_ReturnsNotFound()
    {
        var adminUserId = Guid.NewGuid();
        var client = CreateAdminClient(adminUserId);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();

            db.Users.Add(new User { Id = adminUserId, Email = "admin@example.com", PasswordHash = "hash", Role = "admin", IsActive = true });
            db.SaveChanges();
        }

        var response = await client.GetAsync($"/api/admin/users/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeactivateUser_WhenActive_ReturnsOk_AndSetsInactive()
    {
        var adminUserId = Guid.NewGuid();
        var client = CreateAdminClient(adminUserId);
        Guid targetUserId;

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();

            db.Users.Add(new User { Id = adminUserId, Email = "admin@example.com", PasswordHash = "hash", Role = "admin", IsActive = true });

            var user = new User { Id = Guid.NewGuid(), Email = "target@example.com", PasswordHash = "hash", Role = "user", IsActive = true };
            db.Users.Add(user);
            db.SaveChanges();
            targetUserId = user.Id;
        }

        var response = await client.PatchAsJsonAsync($"/api/admin/users/{targetUserId}/deactivate", new { reason = "test" });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var user = await db.Users.FindAsync(targetUserId);
            Assert.NotNull(user);
            Assert.False(user.IsActive);
        }
    }

    [Fact]
    public async Task ReactivateUser_WhenInactive_ReturnsOk_AndSetsActive()
    {
        var adminUserId = Guid.NewGuid();
        var client = CreateAdminClient(adminUserId);
        Guid targetUserId;

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();

            db.Users.Add(new User { Id = adminUserId, Email = "admin@example.com", PasswordHash = "hash", Role = "admin", IsActive = true });

            var user = new User { Id = Guid.NewGuid(), Email = "target@example.com", PasswordHash = "hash", Role = "user", IsActive = false };
            db.Users.Add(user);
            db.SaveChanges();
            targetUserId = user.Id;
        }

        var response = await client.PatchAsync($"/api/admin/users/{targetUserId}/reactivate", content: null);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var user = await db.Users.FindAsync(targetUserId);
            Assert.NotNull(user);
            Assert.True(user.IsActive);
        }
    }
}

public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int Total { get; set; }
    public int Skip { get; set; }
    public int Take { get; set; }
}

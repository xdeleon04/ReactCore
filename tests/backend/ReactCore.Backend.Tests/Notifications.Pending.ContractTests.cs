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
using ReactCore.Backend.Services;
using ReactCore.Tests;

namespace ReactCore.Backend.Tests.Contract;

public class NotificationsPendingContractTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private const string TestJwtKey = "SuperSecretKeyForTesting12345!@#$%";
    private const string TestIssuer = "TestIssuer";
    private const string TestAudience = "TestAudience";

    private readonly WebApplicationFactory<Program> _factory;

    public NotificationsPendingContractTests(CustomWebApplicationFactory<Program> factory)
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

    [Fact]
    public async Task GetPending_WithoutAuth_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/notifications/pending");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetPending_WithAuth_ReturnsTriggeredAndMarksNotified()
    {
        var userId = Guid.NewGuid();
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", GenerateValidToken(userId));

        int notificationId;

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();

            db.Users.Add(new User { Id = userId, Email = $"pending_{userId:N}@example.com", PasswordHash = "x" });

            var product = new Product
            {
                Name = $"Pending_{Guid.NewGuid():N}",
                Category = "Electronics",
                Price = 10m,
                StockQuantity = 3,
                ReorderLevel = 1,
                IsDeleted = false
            };
            db.Products.Add(product);
            db.SaveChanges();

            var pref = new NotificationPreferences
            {
                UserId = userId,
                ProductId = product.Id,
                NotificationType = "OutOfStockNotification",
                CreatedAt = DateTime.UtcNow,
                NotifiedAt = null
            };

            db.NotificationPreferences.Add(pref);
            db.SaveChanges();
            notificationId = pref.Id;
        }

        var response = await client.GetAsync("/api/notifications/pending");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var items = await response.Content.ReadFromJsonAsync<List<TriggeredNotificationDto>>();
        Assert.NotNull(items);
        Assert.NotEmpty(items);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var refreshed = db.NotificationPreferences.Single(np => np.Id == notificationId);
            Assert.NotNull(refreshed.NotifiedAt);
        }
    }

    private static string GenerateValidToken(Guid userId)
    {
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestJwtKey));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, "test@example.com"),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: TestIssuer,
            audience: TestAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(15),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

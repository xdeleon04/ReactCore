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
using ReactCore.Backend.Models.Dto;
using ReactCore.Tests;

namespace ReactCore.Backend.Tests.Integration;

public class AdminDashboardAuditWorkflowTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private const string TestJwtKey = "SuperSecretKeyForTesting12345!@#$%";
    private const string TestIssuer = "TestIssuer";
    private const string TestAudience = "TestAudience";

    private readonly WebApplicationFactory<Program> _factory;

    public AdminDashboardAuditWorkflowTests(CustomWebApplicationFactory<Program> factory)
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

    private HttpClient CreateAdminClient(Guid adminUserId)
    {
        var client = _factory.CreateClient();

        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
            "Bearer",
            GenerateToken(adminUserId, "admin@example.com", "admin"));

        return client;
    }

    [Fact]
    public async Task DashboardSummary_ReflectsSeededCounts()
    {
        var adminUserId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();

            db.Users.Add(new User { Id = adminUserId, Email = "admin@example.com", PasswordHash = "hash", Role = "admin", IsActive = true, CreatedAt = now });

            db.Users.AddRange(
                new User { Id = Guid.NewGuid(), Email = "a1@example.com", PasswordHash = "hash", Role = "user", IsActive = true, CreatedAt = now },
                new User { Id = Guid.NewGuid(), Email = "a2@example.com", PasswordHash = "hash", Role = "user", IsActive = true, CreatedAt = now.AddDays(-10) },
                new User { Id = Guid.NewGuid(), Email = "i1@example.com", PasswordHash = "hash", Role = "user", IsActive = false, CreatedAt = now.AddDays(-50) });

            db.Products.AddRange(
                new Product { Name = "P1", Category = "C", Price = 10, StockQuantity = 2, ReorderLevel = 5, IsDeleted = false },
                new Product { Name = "P2", Category = "C", Price = 10, StockQuantity = 10, ReorderLevel = 5, IsDeleted = false },
                new Product { Name = "P3", Category = "C", Price = 10, StockQuantity = 0, ReorderLevel = 5, IsDeleted = true });

            var customerId = Guid.NewGuid();
            db.Users.Add(new User { Id = customerId, Email = "customer@example.com", PasswordHash = "hash", Role = "user", IsActive = true, CreatedAt = now });

            db.Orders.AddRange(
                new Order { UserId = customerId, Email = "customer@example.com", OrderNumber = "ORD-TODAY-1", Status = "Pending", SubtotalPrice = 3, TotalPrice = 3, CreatedAt = now },
                new Order { UserId = customerId, Email = "customer@example.com", OrderNumber = "ORD-TODAY-2", Status = "Processing", SubtotalPrice = 7, TotalPrice = 7, CreatedAt = now },
                new Order { UserId = customerId, Email = "customer@example.com", OrderNumber = "ORD-YEST", Status = "Completed", SubtotalPrice = 9, TotalPrice = 9, CreatedAt = now.AddDays(-1) });

            db.SaveChanges();
        }

        var client = CreateAdminClient(adminUserId);
        var response = await client.GetAsync("/api/admin/dashboard/summary");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<DashboardSummaryDto>();
        Assert.NotNull(body);

        Assert.Equal(5, body.UserMetrics.TotalUsers);
        Assert.Equal(4, body.UserMetrics.ActiveUsers);
        Assert.Equal(1, body.UserMetrics.InactiveUsers);

        Assert.Equal(3, body.ProductMetrics.TotalProducts);
        Assert.Equal(2, body.ProductMetrics.ActiveProducts);
        Assert.Equal(1, body.ProductMetrics.ArchivedProducts);
        Assert.Equal(1, body.ProductMetrics.LowStockCount);

        Assert.Equal(1, body.OrderMetrics.PendingOrders);
        Assert.Equal(1, body.OrderMetrics.ProcessingOrders);
        Assert.Equal(2, body.OrderMetrics.TotalOrdersToday);
        Assert.Equal(10m, body.OrderMetrics.TodayRevenue);
    }

    [Fact]
    public async Task AuditLogs_List_RespectsDateFilters()
    {
        var adminUserId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();

            db.Users.Add(new User { Id = adminUserId, Email = "admin@example.com", PasswordHash = "hash", Role = "admin", IsActive = true });

            db.AdminActions.AddRange(
                new AdminAction { AdminUserId = adminUserId, ActionType = "ProductUpdate", EntityType = "Product", EntityId = "1", Timestamp = now.AddDays(-10) },
                new AdminAction { AdminUserId = adminUserId, ActionType = "UserDeactivate", EntityType = "User", EntityId = Guid.NewGuid().ToString(), Timestamp = now.AddDays(-1) },
                new AdminAction { AdminUserId = adminUserId, ActionType = "OrderStatusChange", EntityType = "Order", EntityId = "10", Timestamp = now }
            );

            db.SaveChanges();
        }

        var client = CreateAdminClient(adminUserId);

        var start = now.AddDays(-2).ToString("O");
        var end = now.AddHours(1).ToString("O");

        var response = await client.GetAsync($"/api/admin/audit-logs?startDate={Uri.EscapeDataString(start)}&endDate={Uri.EscapeDataString(end)}&skip=0&take=100");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<PagedResponse<AdminAuditLogDto>>();
        Assert.NotNull(body);
        Assert.Equal(2, body.Total);
        Assert.All(body.Items, i => Assert.True(i.Timestamp >= now.AddDays(-2)));
        Assert.Contains(body.Items, i => i.Action == "UserDeactivate");
        Assert.Contains(body.Items, i => i.Action == "OrderStatusChange");
    }

    private sealed class PagedResponse<T>
    {
        public List<T> Items { get; set; } = new();
        public int Total { get; set; }
        public int Skip { get; set; }
        public int Take { get; set; }
    }
}

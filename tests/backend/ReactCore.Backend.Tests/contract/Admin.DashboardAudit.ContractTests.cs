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

namespace ReactCore.Backend.Tests.Contract;

public class AdminDashboardAuditContractTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private const string TestJwtKey = "SuperSecretKeyForTesting12345!@#$%";
    private const string TestIssuer = "TestIssuer";
    private const string TestAudience = "TestAudience";

    private readonly WebApplicationFactory<Program> _factory;

    public AdminDashboardAuditContractTests(CustomWebApplicationFactory<Program> factory)
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

    private HttpClient CreateClientWithRole(string role, Guid? userId = null)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
            "Bearer",
            GenerateToken(userId ?? Guid.NewGuid(), role == "admin" ? "admin@example.com" : "user@example.com", role));
        return client;
    }

    [Fact]
    public async Task DashboardSummary_WithoutToken_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/admin/dashboard/summary");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task DashboardSummary_WithNonAdminRole_ReturnsForbidden()
    {
        var client = CreateClientWithRole("user");
        var response = await client.GetAsync("/api/admin/dashboard/summary");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task DashboardSummary_WithAdminRole_ReturnsOk_AndMatchesContractShape()
    {
        var adminUserId = Guid.NewGuid();
        var client = CreateClientWithRole("admin", adminUserId);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();

            db.Users.Add(new User { Id = adminUserId, Email = "admin@example.com", PasswordHash = "hash", Role = "admin", IsActive = true, CreatedAt = DateTime.UtcNow });
            db.Users.AddRange(
                new User { Id = Guid.NewGuid(), Email = "active1@example.com", PasswordHash = "hash", Role = "user", IsActive = true, CreatedAt = DateTime.UtcNow },
                new User { Id = Guid.NewGuid(), Email = "inactive1@example.com", PasswordHash = "hash", Role = "user", IsActive = false, CreatedAt = DateTime.UtcNow.AddDays(-40) });

            db.Products.AddRange(
                new Product { Name = "P1", Category = "C", Price = 10, StockQuantity = 1, ReorderLevel = 5, IsDeleted = false },
                new Product { Name = "P2", Category = "C", Price = 10, StockQuantity = 10, ReorderLevel = 5, IsDeleted = false },
                new Product { Name = "P3", Category = "C", Price = 10, StockQuantity = 0, ReorderLevel = 5, IsDeleted = true });

            var customerId = Guid.NewGuid();
            db.Users.Add(new User { Id = customerId, Email = "customer@example.com", PasswordHash = "hash", Role = "user", IsActive = true, CreatedAt = DateTime.UtcNow });

            db.Orders.AddRange(
                new Order { UserId = customerId, Email = "customer@example.com", OrderNumber = "ORD-1", Status = "Pending", SubtotalPrice = 10, TotalPrice = 10, CreatedAt = DateTime.UtcNow },
                new Order { UserId = customerId, Email = "customer@example.com", OrderNumber = "ORD-2", Status = "Processing", SubtotalPrice = 5, TotalPrice = 5, CreatedAt = DateTime.UtcNow });

            db.SaveChanges();
        }

        var response = await client.GetAsync("/api/admin/dashboard/summary");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<DashboardSummaryDto>();
        Assert.NotNull(body);
        Assert.NotNull(body.UserMetrics);
        Assert.NotNull(body.ProductMetrics);
        Assert.NotNull(body.OrderMetrics);

        Assert.Equal(4, body.UserMetrics.TotalUsers);
        Assert.Equal(3, body.UserMetrics.ActiveUsers);
        Assert.Equal(1, body.UserMetrics.InactiveUsers);

        Assert.Equal(3, body.ProductMetrics.TotalProducts);
        Assert.Equal(2, body.ProductMetrics.ActiveProducts);
        Assert.Equal(1, body.ProductMetrics.ArchivedProducts);
        Assert.Equal(1, body.ProductMetrics.LowStockCount);

        Assert.Equal(1, body.OrderMetrics.PendingOrders);
        Assert.Equal(1, body.OrderMetrics.ProcessingOrders);
        Assert.Equal(2, body.OrderMetrics.TotalOrdersToday);
        Assert.Equal(15m, body.OrderMetrics.TodayRevenue);
    }

    [Fact]
    public async Task AuditLogsList_WithoutToken_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/admin/audit-logs");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AuditLogsList_WithNonAdminRole_ReturnsForbidden()
    {
        var client = CreateClientWithRole("user");
        var response = await client.GetAsync("/api/admin/audit-logs");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AuditLogsList_WithAdminRole_ReturnsOk_AndIncludesReason()
    {
        var adminUserId = Guid.NewGuid();
        var client = CreateClientWithRole("admin", adminUserId);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();

            db.Users.Add(new User { Id = adminUserId, Email = "admin@example.com", PasswordHash = "hash", Role = "admin", IsActive = true });

            db.AdminActions.AddRange(
                new AdminAction
                {
                    AdminUserId = adminUserId,
                    ActionType = "ProductUpdate",
                    EntityType = "Product",
                    EntityId = "1",
                    Timestamp = DateTime.UtcNow.AddMinutes(-1),
                    OldValues = "{\"price\": 99.99}",
                    NewValues = "{\"price\": 89.99}"
                },
                new AdminAction
                {
                    AdminUserId = adminUserId,
                    ActionType = "UserDeactivate",
                    EntityType = "User",
                    EntityId = Guid.NewGuid().ToString(),
                    Timestamp = DateTime.UtcNow.AddMinutes(-2),
                    OldValues = "{\"isActive\": true}",
                    NewValues = "{\"isActive\": false}",
                    Reason = "User reported suspicious activity"
                });

            db.SaveChanges();
        }

        var response = await client.GetAsync("/api/admin/audit-logs?skip=0&take=50");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<PagedResponse<AdminAuditLogDto>>();
        Assert.NotNull(body);
        Assert.Equal(2, body.Total);
        Assert.Equal(2, body.Items.Count);

        Assert.Contains(body.Items, i => i.Action == "UserDeactivate" && i.Reason == "User reported suspicious activity");
    }

    [Fact]
    public async Task AuditLogDetail_WithAdminRole_ReturnsOk_AndIncludesIpAndReason()
    {
        var adminUserId = Guid.NewGuid();
        var client = CreateClientWithRole("admin", adminUserId);
        int auditId;

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();

            db.Users.Add(new User { Id = adminUserId, Email = "admin@example.com", PasswordHash = "hash", Role = "admin", IsActive = true });

            var action = new AdminAction
            {
                AdminUserId = adminUserId,
                ActionType = "ProductUpdate",
                EntityType = "Product",
                EntityId = "1",
                Timestamp = DateTime.UtcNow,
                IpAddress = "192.168.1.100",
                Reason = "Q4 price adjustment",
                OldValues = "{\"price\": 99.99}",
                NewValues = "{\"price\": 89.99}"
            };
            db.AdminActions.Add(action);
            db.SaveChanges();
            auditId = action.Id;
        }

        var response = await client.GetAsync($"/api/admin/audit-logs/{auditId}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<AdminAuditLogDetailDto>();
        Assert.NotNull(body);
        Assert.Equal(auditId, body.Id);
        Assert.Equal(adminUserId, body.AdminId);
        Assert.Equal("admin@example.com", body.AdminEmail);
        Assert.Equal("192.168.1.100", body.IpAddress);
        Assert.Equal("Q4 price adjustment", body.Reason);
    }

    private sealed class PagedResponse<T>
    {
        public List<T> Items { get; set; } = new();
        public int Total { get; set; }
        public int Skip { get; set; }
        public int Take { get; set; }
    }
}

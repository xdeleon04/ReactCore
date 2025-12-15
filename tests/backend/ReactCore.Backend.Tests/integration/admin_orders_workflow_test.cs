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
using ReactCore.Backend.Models.Enums;
using ReactCore.Tests;

namespace ReactCore.Backend.Tests.Integration;

public class AdminOrdersWorkflowTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private const string TestJwtKey = "SuperSecretKeyForTesting12345!@#$%";
    private const string TestIssuer = "TestIssuer";
    private const string TestAudience = "TestAudience";

    private readonly WebApplicationFactory<Program> _factory;

    public AdminOrdersWorkflowTests(CustomWebApplicationFactory<Program> factory)
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
    public async Task AdminOrders_StatusUpdate_ForwardOnly_UpdatesAndWritesAudit()
    {
        var adminUserId = Guid.NewGuid();
        var customerUserId = Guid.NewGuid();
        int orderId;

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();

            db.Users.Add(new User { Id = adminUserId, Email = "admin@example.com", PasswordHash = "hash", Role = "admin", IsActive = true });
            db.Users.Add(new User { Id = customerUserId, Email = "customer@example.com", PasswordHash = "hash", Role = "user", IsActive = true });

            var order = new Order
            {
                UserId = customerUserId,
                Email = "customer@example.com",
                OrderNumber = "ORD-1",
                Status = "Processing",
                SubtotalPrice = 10,
                TotalPrice = 10,
                Items = new List<OrderItem>
                {
                    new() { ProductId = 1, ProductName = "Thing", Quantity = 1, UnitPrice = 10, LineTotal = 10 }
                }
            };

            db.Orders.Add(order);
            db.SaveChanges();
            orderId = order.Id;
        }

        var client = CreateAdminClient(adminUserId);

        var response = await client.PatchAsJsonAsync($"/api/admin/orders/{orderId}/status", new
        {
            status = "Shipped",
            reason = "integration"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var updated = await db.Orders.FindAsync(orderId);
            Assert.NotNull(updated);
            Assert.Equal("Shipped", updated!.Status);

            Assert.Contains(db.AdminActions, a =>
                a.AdminUserId == adminUserId &&
                a.ActionType == AdminActionType.OrderStatusChange.ToString() &&
                a.EntityType == EntityType.Order.ToString() &&
                a.EntityId == orderId.ToString());
        }
    }

    [Fact]
    public async Task AdminOrders_StatusUpdate_BackwardTransition_Returns400_DoesNotAuditOrChange()
    {
        var adminUserId = Guid.NewGuid();
        var customerUserId = Guid.NewGuid();
        int orderId;

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();

            db.Users.Add(new User { Id = adminUserId, Email = "admin@example.com", PasswordHash = "hash", Role = "admin", IsActive = true });
            db.Users.Add(new User { Id = customerUserId, Email = "customer@example.com", PasswordHash = "hash", Role = "user", IsActive = true });

            var order = new Order
            {
                UserId = customerUserId,
                Email = "customer@example.com",
                OrderNumber = "ORD-1",
                Status = "Completed",
                SubtotalPrice = 10,
                TotalPrice = 10
            };

            db.Orders.Add(order);
            db.SaveChanges();
            orderId = order.Id;
        }

        var client = CreateAdminClient(adminUserId);

        var response = await client.PatchAsJsonAsync($"/api/admin/orders/{orderId}/status", new
        {
            status = "Processing",
            reason = "integration"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var updated = await db.Orders.FindAsync(orderId);
            Assert.NotNull(updated);
            Assert.Equal("Completed", updated!.Status);

            Assert.DoesNotContain(db.AdminActions, a => a.EntityType == EntityType.Order.ToString() && a.EntityId == orderId.ToString());
        }
    }
}

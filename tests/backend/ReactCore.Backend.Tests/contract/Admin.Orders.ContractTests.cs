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

namespace ReactCore.Backend.Tests.Contract;

public class AdminOrdersContractTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private const string TestJwtKey = "SuperSecretKeyForTesting12345!@#$%";
    private const string TestIssuer = "TestIssuer";
    private const string TestAudience = "TestAudience";

    private readonly WebApplicationFactory<Program> _factory;

    public AdminOrdersContractTests(CustomWebApplicationFactory<Program> factory)
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

    private HttpClient CreateAdminClient()
    {
        var client = _factory.CreateClient();

        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
            "Bearer",
            GenerateToken(Guid.NewGuid(), "admin@example.com", "admin"));

        return client;
    }

    [Fact]
    public async Task GetAdminOrders_WithoutToken_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/admin/orders");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetAdminOrders_WithNonAdminRole_ReturnsForbidden()
    {
        var client = _factory.CreateClient();

        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
            "Bearer",
            GenerateToken(Guid.NewGuid(), "user@example.com", "user"));

        var response = await client.GetAsync("/api/admin/orders");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetAdminOrders_WithAdminRole_ReturnsOk_AndPaginates()
    {
        var client = CreateAdminClient();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();

            var now = DateTime.UtcNow;

            db.Orders.AddRange(
                new Order
                {
                    UserId = Guid.NewGuid(),
                    Email = "john@example.com",
                    OrderNumber = "ORD-20241214-001",
                    Status = "Processing",
                    CreatedAt = now.AddDays(-1),
                    UpdatedAt = now.AddDays(-1),
                    SubtotalPrice = 100,
                    TotalPrice = 100,
                    Items = new List<OrderItem>
                    {
                        new() { ProductId = 1, ProductName = "Laptop", Quantity = 1, UnitPrice = 100, LineTotal = 100 }
                    }
                },
                new Order
                {
                    UserId = Guid.NewGuid(),
                    Email = "jane@example.com",
                    OrderNumber = "ORD-20241214-002",
                    Status = "Pending",
                    CreatedAt = now.AddDays(-2),
                    UpdatedAt = now.AddDays(-2),
                    SubtotalPrice = 50,
                    TotalPrice = 50,
                    Items = new List<OrderItem>
                    {
                        new() { ProductId = 2, ProductName = "Cable", Quantity = 5, UnitPrice = 10, LineTotal = 50 }
                    }
                });

            db.SaveChanges();
        }

        var response = await client.GetAsync("/api/admin/orders?skip=0&take=20");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<PagedResult<AdminOrderListItem>>();
        Assert.NotNull(body);
        Assert.True(body.Take <= 100);
        Assert.True(body.Total >= body.Items.Count);
        Assert.NotEmpty(body.Items);
    }

    [Fact]
    public async Task GetAdminOrders_WithStatusFilter_ReturnsOnlyMatches()
    {
        var client = CreateAdminClient();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();

            db.Orders.AddRange(
                new Order { UserId = Guid.NewGuid(), Email = "john@example.com", OrderNumber = "ORD-1", Status = "Processing", SubtotalPrice = 10, TotalPrice = 10 },
                new Order { UserId = Guid.NewGuid(), Email = "jane@example.com", OrderNumber = "ORD-2", Status = "Completed", SubtotalPrice = 20, TotalPrice = 20 });

            db.SaveChanges();
        }

        var response = await client.GetAsync("/api/admin/orders?status=Processing&skip=0&take=100");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<PagedResult<AdminOrderListItem>>();
        Assert.NotNull(body);
        Assert.All(body.Items, o => Assert.Equal("Processing", o.Status));
    }

    [Fact]
    public async Task GetAdminOrderDetail_WhenMissing_ReturnsNotFound()
    {
        var client = CreateAdminClient();

        var response = await client.GetAsync("/api/admin/orders/999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetAdminOrderDetail_ReturnsItems_AndAllowedTransitions()
    {
        var client = CreateAdminClient();
        int orderId;

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();

            var order = new Order
            {
                UserId = Guid.NewGuid(),
                Email = "john@example.com",
                OrderNumber = "ORD-20241214-001",
                Status = "Processing",
                SubtotalPrice = 1349.94m,
                TotalPrice = 1349.94m,
                Items = new List<OrderItem>
                {
                    new() { ProductId = 1, ProductName = "Laptop Pro 15\"", Quantity = 1, UnitPrice = 1299.99m, LineTotal = 1299.99m },
                    new() { ProductId = 12, ProductName = "USB Cable", Quantity = 5, UnitPrice = 9.99m, LineTotal = 49.95m },
                }
            };

            db.Orders.Add(order);
            db.SaveChanges();
            orderId = order.Id;
        }

        var response = await client.GetAsync($"/api/admin/orders/{orderId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<AdminOrderDetailResponse>();
        Assert.NotNull(body);
        Assert.Equal(orderId, body.Id);
        Assert.Equal("Processing", body.Status);
        Assert.NotEmpty(body.Items);
        Assert.Equal(body.Subtotal, body.Total);
        Assert.Contains("Shipped", body.AllowedStatusTransitions);
    }

    [Fact]
    public async Task UpdateOrderStatus_WithValidTransition_ReturnsOk()
    {
        var client = CreateAdminClient();
        int orderId;

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();

            var order = new Order
            {
                UserId = Guid.NewGuid(),
                Email = "john@example.com",
                OrderNumber = "ORD-1",
                Status = "Processing",
                SubtotalPrice = 10,
                TotalPrice = 10
            };

            db.Orders.Add(order);
            db.SaveChanges();
            orderId = order.Id;
        }

        var response = await client.PatchAsJsonAsync($"/api/admin/orders/{orderId}/status", new
        {
            status = "Shipped",
            reason = "Order shipped"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<OrderStatusUpdateResponse>();
        Assert.NotNull(body);
        Assert.True(body.Success);
        Assert.Equal(orderId, body.OrderId);
        Assert.Equal("Shipped", body.Status);
        Assert.NotEqual(default, body.UpdatedAt);
    }

    [Fact]
    public async Task UpdateOrderStatus_WithBackwardTransition_ReturnsBadRequest()
    {
        var client = CreateAdminClient();
        int orderId;

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();

            var order = new Order
            {
                UserId = Guid.NewGuid(),
                Email = "john@example.com",
                OrderNumber = "ORD-1",
                Status = "Completed",
                SubtotalPrice = 10,
                TotalPrice = 10
            };

            db.Orders.Add(order);
            db.SaveChanges();
            orderId = order.Id;
        }

        var response = await client.PatchAsJsonAsync($"/api/admin/orders/{orderId}/status", new
        {
            status = "Processing",
            reason = "Trying to revert"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<StatusTransitionError>();
        Assert.NotNull(body);
        Assert.Equal("Invalid status transition", body.Error);
        Assert.Equal(400, body.StatusCode);
        Assert.Contains("Cannot transition", body.Message);
    }

    public class AdminOrderListItem
    {
        public int Id { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public decimal Total { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class AdminOrderItem
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal LineTotal { get; set; }
    }

    public class AdminOrderDetailResponse
    {
        public int Id { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public List<AdminOrderItem> Items { get; set; } = new();
        public decimal Subtotal { get; set; }
        public decimal Total { get; set; }
        public List<string> AllowedStatusTransitions { get; set; } = new();
    }

    public class OrderStatusUpdateResponse
    {
        public bool Success { get; set; }
        public int OrderId { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime UpdatedAt { get; set; }
    }

    public class StatusTransitionError
    {
        public string Error { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public int StatusCode { get; set; }
    }
}

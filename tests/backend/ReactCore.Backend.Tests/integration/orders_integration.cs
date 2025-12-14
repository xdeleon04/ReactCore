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
using ReactCore.Tests;
using Xunit;

namespace ReactCore.Backend.Tests.Integration;

public class OrdersIntegrationTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private const string TestJwtKey = "SuperSecretKeyForTesting12345!@#$%";
    private const string TestIssuer = "TestIssuer";
    private const string TestAudience = "TestAudience";

    private readonly WebApplicationFactory<Program> _factory;

    public OrdersIntegrationTests(CustomWebApplicationFactory<Program> factory)
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
    public async Task Orders_CreateAndGet_ReturnsOrderDetails()
    {
        var userId = Guid.NewGuid();
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", GenerateValidToken(userId));

        int productId;

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();

            db.Users.Add(new User { Id = userId, Email = $"orders_{userId:N}@example.com", PasswordHash = "x" });
            var product = new Product { Name = $"OrderProduct_{Guid.NewGuid():N}", Category = "Electronics", Price = 50m, StockQuantity = 10 };
            db.Products.Add(product);
            db.SaveChanges();
            productId = product.Id;
        }

        var add = await client.PostAsJsonAsync("/api/carts/items", new { productId, quantity = 2 });
        Assert.Equal(HttpStatusCode.Created, add.StatusCode);

        var cart = await client.GetFromJsonAsync<CartDto>("/api/carts/current");
        Assert.NotNull(cart);
        Assert.True(cart.Id > 0);

        var create = await client.PostAsJsonAsync("/api/orders", new { email = "customer@example.com", cartId = cart.Id });
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);

        var order = await create.Content.ReadFromJsonAsync<OrderDto>();
        Assert.NotNull(order);
        Assert.StartsWith($"ORD-{DateTime.UtcNow:yyyyMMdd}-", order.OrderNumber);
        Assert.Equal("Pending", order.Status);
        Assert.True(order.Items.Count > 0);

        var get = await client.GetAsync($"/api/orders/{order.OrderNumber}");
        Assert.Equal(HttpStatusCode.OK, get.StatusCode);

        var fetched = await get.Content.ReadFromJsonAsync<OrderDto>();
        Assert.NotNull(fetched);
        Assert.Equal(order.OrderNumber, fetched.OrderNumber);
    }

    [Fact]
    public async Task Orders_Create_WhenInventoryChanged_ReturnsConflictWithConflicts()
    {
        var userId = Guid.NewGuid();
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", GenerateValidToken(userId));

        int productId;

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();

            db.Users.Add(new User { Id = userId, Email = $"conflict_{userId:N}@example.com", PasswordHash = "x" });
            var product = new Product { Name = $"ConflictProduct_{Guid.NewGuid():N}", Category = "Electronics", Price = 50m, StockQuantity = 5 };
            db.Products.Add(product);
            db.SaveChanges();
            productId = product.Id;
        }

        var add = await client.PostAsJsonAsync("/api/carts/items", new { productId, quantity = 5 });
        Assert.Equal(HttpStatusCode.Created, add.StatusCode);

        var cart = await client.GetFromJsonAsync<CartDto>("/api/carts/current");
        Assert.NotNull(cart);

        // Simulate inventory changing after item added to cart.
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var product = await db.Products.FirstAsync(p => p.Id == productId);
            product.StockQuantity = 1;
            await db.SaveChangesAsync();
        }

        var create = await client.PostAsJsonAsync("/api/orders", new { email = "customer@example.com", cartId = cart.Id });
        Assert.Equal(HttpStatusCode.Conflict, create.StatusCode);

        var conflict = await create.Content.ReadFromJsonAsync<CreateOrderConflictResponse>();
        Assert.NotNull(conflict);
        Assert.Equal(409, conflict.StatusCode);
        Assert.NotEmpty(conflict.Conflicts);
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

    private sealed record CartItemDto(int Id, int ProductId, string ProductName, int Quantity, decimal UnitPrice, decimal LineTotal, string? ImageUrl);

    private sealed record CartDto(int Id, Guid UserId, List<CartItemDto> Items, int ItemCount, decimal Subtotal, decimal Total, DateTime CreatedAt, DateTime UpdatedAt);

    private sealed record OrderItemDto(int Id, int ProductId, string ProductName, int Quantity, decimal UnitPrice, decimal LineTotal);

    private sealed record OrderDto(int Id, string OrderNumber, Guid UserId, string Email, decimal Subtotal, decimal Total, string Status, List<OrderItemDto> Items, DateTime CreatedAt, DateTime? UpdatedAt);

    private sealed record InventoryConflictDto(int CartItemId, int ProductId, string ProductName, int RequestedQuantity, int AvailableQuantity, string Action);

    private sealed record CreateOrderConflictResponse(int StatusCode, string Message, List<InventoryConflictDto> Conflicts);
}

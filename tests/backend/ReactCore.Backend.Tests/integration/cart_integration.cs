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

public class CartIntegrationTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private const string TestJwtKey = "SuperSecretKeyForTesting12345!@#$%";
    private const string TestIssuer = "TestIssuer";
    private const string TestAudience = "TestAudience";

    private readonly WebApplicationFactory<Program> _factory;

    public CartIntegrationTests(CustomWebApplicationFactory<Program> factory)
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
    public async Task Cart_FullFlow_AddGetUpdateRemove()
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

            db.Users.Add(new User { Id = userId, Email = $"cart_{userId:N}@example.com", PasswordHash = "x" });
            var product = new Product { Name = $"CartProduct_{Guid.NewGuid():N}", Category = "Electronics", Price = 25m, StockQuantity = 10 };
            db.Products.Add(product);
            db.SaveChanges();
            productId = product.Id;
        }

        var addResponse = await client.PostAsJsonAsync("/api/carts/items", new { productId, quantity = 2 });
        Assert.Equal(HttpStatusCode.Created, addResponse.StatusCode);
        var added = await addResponse.Content.ReadFromJsonAsync<AddToCartResponse>();
        Assert.NotNull(added);
        Assert.True(added.Success);
        Assert.True(added.CartItemId > 0);

        var getResponse = await client.GetAsync("/api/carts/current");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var cart = await getResponse.Content.ReadFromJsonAsync<CartDto>();
        Assert.NotNull(cart);
        var item = Assert.Single(cart.Items);
        Assert.Equal(2, item.Quantity);

        var updateResponse = await client.PutAsJsonAsync($"/api/carts/items/{added.CartItemId}", new { quantity = 3 });
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        var refreshed = await client.GetFromJsonAsync<CartDto>("/api/carts/current");
        Assert.NotNull(refreshed);
        Assert.Equal(3, Assert.Single(refreshed.Items).Quantity);

        var deleteResponse = await client.DeleteAsync($"/api/carts/items/{added.CartItemId}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var empty = await client.GetFromJsonAsync<CartDto>("/api/carts/current");
        Assert.NotNull(empty);
        Assert.Empty(empty.Items);
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

    private sealed record AddToCartResponse(bool Success, int CartId, int CartItemId, int ItemCount, decimal Subtotal);

    private sealed record CartItemDto(int Id, int ProductId, string ProductName, int Quantity, decimal UnitPrice, decimal LineTotal, string? ImageUrl);

    private sealed record CartDto(int Id, Guid UserId, List<CartItemDto> Items, int ItemCount, decimal Subtotal, decimal Total, DateTime CreatedAt, DateTime UpdatedAt);
}

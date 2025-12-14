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

public class ProductsInventoryContractTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private const string TestJwtKey = "SuperSecretKeyForTesting12345!@#$%";
    private const string TestIssuer = "TestIssuer";
    private const string TestAudience = "TestAudience";

    private readonly WebApplicationFactory<Program> _factory;

    public ProductsInventoryContractTests(CustomWebApplicationFactory<Program> factory)
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
    public async Task GetInventory_WithValidId_ReturnsOk()
    {
        var client = _factory.CreateClient();
        int productId;

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();

            var product = new Product
            {
                Name = $"Inv_{Guid.NewGuid():N}",
                Category = "Electronics",
                Price = 10m,
                StockQuantity = 5,
                ReorderLevel = 2,
                IsDeleted = false
            };
            db.Products.Add(product);
            db.SaveChanges();
            productId = product.Id;
        }

        var response = await client.GetAsync($"/api/products/{productId}/inventory");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<InventoryStatusResponse>();
        Assert.NotNull(body);
        Assert.Equal(productId, body.ProductId);
        Assert.Equal(5, body.StockQuantity);
        Assert.False(string.IsNullOrWhiteSpace(body.Status));
    }

    [Fact]
    public async Task GetInventory_WithInvalidId_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/products/0/inventory");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetInventory_WithMissingProduct_ReturnsNotFound()
    {
        var client = _factory.CreateClient();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();
        }

        var response = await client.GetAsync("/api/products/999999/inventory");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}

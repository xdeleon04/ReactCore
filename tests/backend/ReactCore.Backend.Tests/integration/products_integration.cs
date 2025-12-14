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

public class ProductsIntegrationTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private const string TestJwtKey = "SuperSecretKeyForTesting12345!@#$%";
    private const string TestIssuer = "TestIssuer";
    private const string TestAudience = "TestAudience";

    private readonly WebApplicationFactory<Program> _factory;

    public ProductsIntegrationTests(CustomWebApplicationFactory<Program> factory)
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
    public async Task GetProducts_WithCategoryFilter_ReturnsOnlyMatching()
    {
        var client = _factory.CreateClient();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();

            db.Products.AddRange(
                new Product { Name = $"Electronics_{Guid.NewGuid():N}", Category = "Electronics", Price = 10m, StockQuantity = 5 },
                new Product { Name = $"Sports_{Guid.NewGuid():N}", Category = "Sports", Price = 12m, StockQuantity = 5 },
                new Product { Name = $"Electronics_{Guid.NewGuid():N}", Category = "Electronics", Price = 15m, StockQuantity = 5 }
            );
            db.SaveChanges();
        }

        var response = await client.GetAsync("/api/products?category=Electronics&skip=0&take=20");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadFromJsonAsync<ProductListResponse>();
        Assert.NotNull(json);
        Assert.All(json.Items, p => Assert.Equal("Electronics", p.Category));
    }

    [Fact]
    public async Task GetProductById_ReturnsDetails()
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
                Name = $"Laptop_{Guid.NewGuid():N}",
                Category = "Electronics",
                Price = 999.99m,
                StockQuantity = 5,
                Specifications = "{\"cpu\":\"i7\"}",
                ImageUrl = "https://example.com/laptop.jpg"
            };
            db.Products.Add(product);
            db.SaveChanges();

            productId = product.Id;
        }

        var response = await client.GetAsync($"/api/products/{productId}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var detail = await response.Content.ReadFromJsonAsync<ProductDetail>();
        Assert.NotNull(detail);
        Assert.Equal(productId, detail.Id);
        Assert.Equal("Electronics", detail.Category);
        Assert.False(string.IsNullOrWhiteSpace(detail.Status));
    }

    [Fact]
    public async Task GetRelatedProducts_ReturnsUpToFour()
    {
        var client = _factory.CreateClient();
        int productId;

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();

            var category = "Electronics";
            var products = Enumerable.Range(0, 6)
                .Select(i => new Product
                {
                    Name = $"Related_{i}_{Guid.NewGuid():N}",
                    Category = category,
                    Price = 10 + i,
                    StockQuantity = 5
                })
                .ToList();

            db.Products.AddRange(products);
            db.SaveChanges();

            productId = products[0].Id;
        }

        var response = await client.GetAsync($"/api/products/{productId}/related");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var related = await response.Content.ReadFromJsonAsync<List<ProductListItem>>();
        Assert.NotNull(related);
        Assert.True(related.Count <= 4);
        Assert.DoesNotContain(related, p => p.Id == productId);
    }

    private sealed record ProductListResponse(List<ProductListItem> Items, int Total, int Skip, int Take);

    private sealed record ProductListItem(int Id, string Name, string Category, decimal Price, string? ImageUrl, string Status);

    private sealed record ProductDetail(
        int Id,
        string Name,
        string? Description,
        decimal Price,
        string Category,
        string? ImageUrl,
        IReadOnlyList<string>? ImageUrls,
        Dictionary<string, object>? Specifications,
        int StockQuantity,
        int ReorderLevel,
        string Status,
        DateTime CreatedAt,
        DateTime? UpdatedAt,
        IReadOnlyList<ProductListItem>? RelatedProducts);
}

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

public class AdminProductsContractTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private const string TestJwtKey = "SuperSecretKeyForTesting12345!@#$%";
    private const string TestIssuer = "TestIssuer";
    private const string TestAudience = "TestAudience";

    private readonly WebApplicationFactory<Program> _factory;

    public AdminProductsContractTests(CustomWebApplicationFactory<Program> factory)
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
    public async Task GetAdminProducts_WithoutToken_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/admin/products");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetAdminProducts_WithNonAdminRole_ReturnsForbidden()
    {
        var client = _factory.CreateClient();

        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
            "Bearer",
            GenerateToken(Guid.NewGuid(), "user@example.com", "user"));

        var response = await client.GetAsync("/api/admin/products");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetAdminProducts_WithAdminRole_ReturnsOk_AndPaginates()
    {
        var client = CreateAdminClient();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();

            db.Products.AddRange(
                new Product { Name = "Laptop", Category = "Electronics", Price = 1299.99m, StockQuantity = 5, ReorderLevel = 5, IsDeleted = false },
                new Product { Name = "Old Cable", Category = "Accessories", Price = 9.99m, StockQuantity = 1, ReorderLevel = 10, IsDeleted = true });

            db.SaveChanges();
        }

        var response = await client.GetAsync("/api/admin/products?skip=0&take=20");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<PagedResult<AdminProductListItem>>();
        Assert.NotNull(body);
        Assert.NotEmpty(body.Items);
        Assert.True(body.Take <= 100);
    }

    [Fact]
    public async Task GetAdminProducts_WithIncludeArchived_ReturnsDeletedToo()
    {
        var client = CreateAdminClient();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();

            db.Products.AddRange(
                new Product { Name = "Active", Category = "Electronics", Price = 10m, StockQuantity = 5, ReorderLevel = 5, IsDeleted = false },
                new Product { Name = "Archived", Category = "Electronics", Price = 20m, StockQuantity = 5, ReorderLevel = 5, IsDeleted = true });

            db.SaveChanges();
        }

        var response = await client.GetAsync("/api/admin/products?isDeleted=true&skip=0&take=50");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<PagedResult<AdminProductListItem>>();
        Assert.NotNull(body);
        Assert.Contains(body.Items, p => p.IsDeleted);
    }

    [Fact]
    public async Task GetAdminProductDetail_WhenMissing_ReturnsNotFound()
    {
        var client = CreateAdminClient();

        var response = await client.GetAsync("/api/admin/products/999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateProduct_WithInvalidPrice_ReturnsBadRequest()
    {
        var client = CreateAdminClient();

        var response = await client.PostAsJsonAsync("/api/admin/products", new
        {
            name = "Bad",
            category = "Accessories",
            price = 0,
            stockQuantity = 1
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateProduct_WithValidBody_ReturnsCreated()
    {
        var client = CreateAdminClient();

        var response = await client.PostAsJsonAsync("/api/admin/products", new
        {
            name = "Wireless Keyboard",
            category = "Accessories",
            price = 79.99,
            stockQuantity = 25,
            description = "Ergonomic",
            reorderLevel = 5,
            specifications = new { wireless = true }
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<AdminProductCreateResponse>();
        Assert.NotNull(body);
        Assert.True(body.Id > 0);
        Assert.Equal("Wireless Keyboard", body.Name);
    }

    [Fact]
    public async Task UpdateProduct_WithInvalidStock_ReturnsBadRequest()
    {
        var client = CreateAdminClient();
        int productId;

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();

            var p = new Product { Name = "Laptop", Category = "Electronics", Price = 10m, StockQuantity = 1, ReorderLevel = 5, IsDeleted = false };
            db.Products.Add(p);
            db.SaveChanges();
            productId = p.Id;
        }

        var response = await client.PutAsJsonAsync($"/api/admin/products/{productId}", new
        {
            name = "Laptop",
            category = "Electronics",
            price = 10,
            stockQuantity = -1,
            reorderLevel = 5
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task DeleteProduct_SoftDeletes_ReturnsNoContent()
    {
        var client = CreateAdminClient();
        int productId;

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();

            var p = new Product { Name = "Laptop", Category = "Electronics", Price = 10m, StockQuantity = 1, ReorderLevel = 5, IsDeleted = false };
            db.Products.Add(p);
            db.SaveChanges();
            productId = p.Id;
        }

        var response = await client.DeleteAsync($"/api/admin/products/{productId}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    public class AdminProductListItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string Category { get; set; } = string.Empty;
        public int StockQuantity { get; set; }
        public int ReorderLevel { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class AdminProductCreateResponse
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string Category { get; set; } = string.Empty;
        public int StockQuantity { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}

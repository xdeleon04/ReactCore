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

public class AdminProductsWorkflowTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private const string TestJwtKey = "SuperSecretKeyForTesting12345!@#$%";
    private const string TestIssuer = "TestIssuer";
    private const string TestAudience = "TestAudience";

    private readonly WebApplicationFactory<Program> _factory;

    public AdminProductsWorkflowTests(CustomWebApplicationFactory<Program> factory)
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
    public async Task AdminProducts_CreateUpdateDelete_WritesAuditEntries()
    {
        var adminUserId = Guid.NewGuid();
        var client = CreateAdminClient(adminUserId);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();
        }

        // Create
        var createResponse = await client.PostAsJsonAsync("/api/admin/products", new
        {
            name = "Widget",
            category = "Accessories",
            price = 19.99,
            stockQuantity = 10,
            reorderLevel = 5
        });

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<CreateResponse>();
        Assert.NotNull(created);
        Assert.True(created.Id > 0);

        // Update
        var updateResponse = await client.PutAsJsonAsync($"/api/admin/products/{created.Id}", new
        {
            name = "Widget V2",
            category = "Accessories",
            price = 29.99,
            stockQuantity = 8,
            reorderLevel = 5
        });

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        // Delete (soft)
        var deleteResponse = await client.DeleteAsync($"/api/admin/products/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var product = await db.Products.FindAsync(created.Id);
            Assert.NotNull(product);
            Assert.True(product!.IsDeleted);

            var audits = db.AdminActions
                .Where(a => a.AdminUserId == adminUserId && a.EntityType == EntityType.Product.ToString() && a.EntityId == created.Id.ToString())
                .ToList();

            Assert.Contains(audits, a => a.ActionType == AdminActionType.ProductCreate.ToString());
            Assert.Contains(audits, a => a.ActionType == AdminActionType.ProductUpdate.ToString());
            Assert.Contains(audits, a => a.ActionType == AdminActionType.ProductDelete.ToString());
        }
    }

    [Fact]
    public async Task AdminProducts_Create_WithInvalidBody_Returns400()
    {
        var client = CreateAdminClient(Guid.NewGuid());

        var response = await client.PostAsJsonAsync("/api/admin/products", new
        {
            name = "",
            category = "",
            price = 0,
            stockQuantity = -1
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private class CreateResponse
    {
        public int Id { get; set; }
    }
}

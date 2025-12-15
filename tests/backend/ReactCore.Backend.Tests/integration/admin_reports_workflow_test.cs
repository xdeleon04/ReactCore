using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using ReactCore.Backend.Data;
using ReactCore.Backend.Models;
using ReactCore.Tests;

namespace ReactCore.Backend.Tests.Integration;

public class AdminReportsWorkflowTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private const string TestJwtKey = "SuperSecretKeyForTesting12345!@#$%";
    private const string TestIssuer = "TestIssuer";
    private const string TestAudience = "TestAudience";

    private readonly WebApplicationFactory<Program> _factory;

    public AdminReportsWorkflowTests(CustomWebApplicationFactory<Program> factory)
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
    public async Task AdminReports_ReturnsAggregates_AndCsvExport()
    {
        var adminUserId = Guid.NewGuid();
        var client = CreateAdminClient(adminUserId);

        var start = new DateTime(2024, 12, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = new DateTime(2024, 12, 31, 23, 59, 59, DateTimeKind.Utc);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();

            var user1 = new User { Id = Guid.NewGuid(), Email = "alice@example.com", PasswordHash = "x" };
            var user2 = new User { Id = Guid.NewGuid(), Email = "bob@example.com", PasswordHash = "x" };

            db.Users.AddRange(user1, user2);

            var widget = new Product { Name = "Widget", Category = "Accessories", Price = 50m, StockQuantity = 100 };
            var cable = new Product { Name = "Cable", Category = "Accessories", Price = 20m, StockQuantity = 100 };

            db.Products.AddRange(widget, cable);
            await db.SaveChangesAsync();

            var order1 = new Order
            {
                UserId = user1.Id,
                OrderNumber = "ORD-20241214-001",
                Email = user1.Email,
                Status = "Processing",
                CreatedAt = new DateTime(2024, 12, 14, 10, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2024, 12, 14, 10, 0, 0, DateTimeKind.Utc),
                SubtotalPrice = 120m,
                TotalPrice = 120m,
                Items = new List<OrderItem>
                {
                    new()
                    {
                        ProductId = widget.Id,
                        ProductName = widget.Name,
                        Quantity = 2,
                        UnitPrice = 50m,
                        LineTotal = 100m
                    },
                    new()
                    {
                        ProductId = cable.Id,
                        ProductName = cable.Name,
                        Quantity = 1,
                        UnitPrice = 20m,
                        LineTotal = 20m
                    }
                }
            };

            var order2 = new Order
            {
                UserId = user2.Id,
                OrderNumber = "ORD-20241213-001",
                Email = user2.Email,
                Status = "Completed",
                CreatedAt = new DateTime(2024, 12, 13, 14, 30, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2024, 12, 13, 14, 30, 0, DateTimeKind.Utc),
                SubtotalPrice = 20m,
                TotalPrice = 20m,
                Items = new List<OrderItem>
                {
                    new()
                    {
                        ProductId = cable.Id,
                        ProductName = cable.Name,
                        Quantity = 1,
                        UnitPrice = 20m,
                        LineTotal = 20m
                    }
                }
            };

            var outsideRangeOrder = new Order
            {
                UserId = user1.Id,
                OrderNumber = "ORD-20250101-001",
                Email = user1.Email,
                Status = "Completed",
                CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                SubtotalPrice = 999m,
                TotalPrice = 999m,
                Items = new List<OrderItem>
                {
                    new()
                    {
                        ProductId = widget.Id,
                        ProductName = widget.Name,
                        Quantity = 1,
                        UnitPrice = 999m,
                        LineTotal = 999m
                    }
                }
            };

            db.Orders.AddRange(order1, order2, outsideRangeOrder);
            await db.SaveChangesAsync();
        }

        var url = $"/api/admin/reports?startDate={Uri.EscapeDataString(start.ToString("o"))}&endDate={Uri.EscapeDataString(end.ToString("o"))}";
        var response = await client.GetAsync(url);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);

        Assert.Equal(start, doc.RootElement.GetProperty("dateRange").GetProperty("start").GetDateTime());
        Assert.Equal(end, doc.RootElement.GetProperty("dateRange").GetProperty("end").GetDateTime());

        var summary = doc.RootElement.GetProperty("summary");
        Assert.Equal(140m, summary.GetProperty("totalRevenue").GetDecimal());
        Assert.Equal(2, summary.GetProperty("totalOrders").GetInt32());
        Assert.Equal(70m, summary.GetProperty("averageOrderValue").GetDecimal());
        Assert.Equal(4, summary.GetProperty("totalItemsSold").GetInt32());
        Assert.Equal(2, summary.GetProperty("uniqueCustomers").GetInt32());

        var topProducts = doc.RootElement.GetProperty("topProducts");
        Assert.True(topProducts.GetArrayLength() >= 2);

        var statusBreakdown = doc.RootElement.GetProperty("orderStatusBreakdown");
        Assert.Equal(2, statusBreakdown.GetArrayLength());

        var csvResponse = await client.GetAsync($"/api/admin/reports/export?startDate={Uri.EscapeDataString(start.ToString("o"))}&endDate={Uri.EscapeDataString(end.ToString("o"))}");
        Assert.Equal(HttpStatusCode.OK, csvResponse.StatusCode);
        Assert.Equal("text/csv", csvResponse.Content.Headers.ContentType?.MediaType);

        var csv = await csvResponse.Content.ReadAsStringAsync();
        Assert.Contains("OrderNumber,Date,CustomerEmail,Subtotal,Total,Status,Items", csv);
        Assert.Contains("ORD-20241214-001", csv);
        Assert.Contains("ORD-20241213-001", csv);
        Assert.DoesNotContain("ORD-20250101-001", csv);
        Assert.Contains("Widget (qty: 2)", csv);
        Assert.Contains("Cable (qty: 1)", csv);
    }
}

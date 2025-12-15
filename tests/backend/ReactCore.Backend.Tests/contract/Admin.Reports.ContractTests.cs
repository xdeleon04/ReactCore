using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using ReactCore.Tests;

namespace ReactCore.Backend.Tests.Contract;

public class AdminReportsContractTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private const string TestJwtKey = "SuperSecretKeyForTesting12345!@#$%";
    private const string TestIssuer = "TestIssuer";
    private const string TestAudience = "TestAudience";

    private readonly WebApplicationFactory<Program> _factory;

    public AdminReportsContractTests(CustomWebApplicationFactory<Program> factory)
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
    public async Task GetReports_WithoutToken_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/admin/reports?startDate=2024-12-01T00:00:00Z&endDate=2024-12-02T00:00:00Z");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetReports_WithNonAdminRole_ReturnsForbidden()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
            "Bearer",
            GenerateToken(Guid.NewGuid(), "user@example.com", "user"));

        var response = await client.GetAsync("/api/admin/reports?startDate=2024-12-01T00:00:00Z&endDate=2024-12-02T00:00:00Z");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetReports_WhenMissingDates_ReturnsBadRequest()
    {
        var client = CreateAdminClient();
        var response = await client.GetAsync("/api/admin/reports");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetReports_WhenInvalidRange_ReturnsBadRequest()
    {
        var client = CreateAdminClient();
        var response = await client.GetAsync("/api/admin/reports?startDate=2024-12-02T00:00:00Z&endDate=2024-12-01T00:00:00Z");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetReports_WithValidRange_ReturnsOk_AndShape()
    {
        var client = CreateAdminClient();
        var response = await client.GetAsync("/api/admin/reports?startDate=2024-12-01T00:00:00Z&endDate=2024-12-14T23:59:59Z");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"dateRange\"", json);
        Assert.Contains("\"summary\"", json);
        Assert.Contains("\"topProducts\"", json);
        Assert.Contains("\"orderStatusBreakdown\"", json);
        Assert.Contains("\"generatedAt\"", json);
    }

    [Fact]
    public async Task ExportReports_WithoutToken_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/admin/reports/export?startDate=2024-12-01T00:00:00Z&endDate=2024-12-02T00:00:00Z");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ExportReports_WithValidRange_ReturnsCsv()
    {
        var client = CreateAdminClient();
        var response = await client.GetAsync("/api/admin/reports/export?startDate=2024-12-01T00:00:00Z&endDate=2024-12-02T00:00:00Z");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/csv", response.Content.Headers.ContentType?.MediaType);

        Assert.NotNull(response.Content.Headers.ContentDisposition);
        Assert.Equal("attachment", response.Content.Headers.ContentDisposition!.DispositionType);

        var csv = await response.Content.ReadAsStringAsync();
        Assert.Contains("OrderNumber,Date,CustomerEmail,Subtotal,Total,Status,Items", csv);
    }

    [Fact]
    public async Task ExportReports_WhenRangeTooLarge_ReturnsBadRequest()
    {
        var client = CreateAdminClient();
        var response = await client.GetAsync("/api/admin/reports/export?startDate=2024-01-01T00:00:00Z&endDate=2026-01-01T00:00:00Z");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}

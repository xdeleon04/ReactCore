using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Moq;
using ReactCore.Backend.Models;
using ReactCore.Backend.Repositories;
using Xunit;

namespace ReactCore.Backend.Tests.Contract;

public class ProtectedEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private const string TestJwtKey = "SuperSecretKeyForTesting12345!@#$%"; // Must be >= 32 chars (256 bits)
    private const string TestIssuer = "TestIssuer";
    private const string TestAudience = "TestAudience";
    private readonly Guid _testUserId = Guid.NewGuid();

    public ProtectedEndpointsTests(WebApplicationFactory<Program> factory)
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((context, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    {"Jwt:Key", TestJwtKey},
                    {"Jwt:Issuer", TestIssuer},
                    {"Jwt:Audience", TestAudience}
                });
            });

            builder.ConfigureTestServices(services =>
            {
                services.AddScoped(_ => _userRepositoryMock.Object);
            });
        });
    }

    private string GenerateValidToken()
    {
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestJwtKey));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, _testUserId.ToString()), // Use ClaimTypes.NameIdentifier to match Controller expectation
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

    [Fact]
    public async Task GetUserProfile_WithValidToken_ReturnsOk()
    {
        // Arrange
        var client = _factory.CreateClient();
        var token = GenerateValidToken();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        _userRepositoryMock.Setup(x => x.GetByIdAsync(_testUserId))
            .ReturnsAsync(new User { Id = _testUserId, Email = "test@example.com", Role = "User" });

        // Act
        var response = await client.GetAsync("/api/user/profile");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetUserProfile_WithoutToken_ReturnsUnauthorized()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/user/profile");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}

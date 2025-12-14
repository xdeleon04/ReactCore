using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using ReactCore.Backend.Models;
using ReactCore.Backend.Services;
using Xunit;

namespace ReactCore.Backend.Tests.Contract;

public class AuthContractTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly Mock<IAuthService> _authServiceMock;

    public AuthContractTests(WebApplicationFactory<Program> factory)
    {
        _authServiceMock = new Mock<IAuthService>();
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.AddScoped(_ => _authServiceMock.Object);
            });
        });
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsOkAndToken()
    {
        // Arrange
        var client = _factory.CreateClient();
        var loginRequest = new { Email = "test@example.com", Password = "Password123!" };

        _authServiceMock.Setup(x => x.LoginAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(("fake-access-token", "fake-refresh-token"));

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(result);
        Assert.Equal("fake-access-token", result.AccessToken);
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ReturnsUnauthorized()
    {
        // Arrange
        var client = _factory.CreateClient();
        var loginRequest = new { Email = "test@example.com", Password = "WrongPassword" };

        _authServiceMock.Setup(x => x.LoginAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(((string, string)?)null);

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithInvalidEmailFormat_ReturnsBadRequest()
    {
        // Arrange
        var client = _factory.CreateClient();
        var loginRequest = new { Email = "invalid-email", Password = "Password123!" };

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}

public class AuthResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
}

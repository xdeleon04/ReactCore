using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace ReactCore.Tests.Contract
{
    public class LogoutContractTests : IClassFixture<CustomWebApplicationFactory<Program>>
    {
        private readonly CustomWebApplicationFactory<Program> _factory;

        public LogoutContractTests(CustomWebApplicationFactory<Program> factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task Logout_ShouldReturnOk_AndClearCookie()
        {
            // Arrange
            var client = _factory.CreateClient();

            // Act
            var response = await client.PostAsync("/api/auth/logout", null);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            // Check for Set-Cookie header to clear the cookie
            // Usually this looks like "refreshToken=; expires=Thu, 01 Jan 1970 00:00:00 GMT"
            var setCookieHeaders = response.Headers.GetValues("Set-Cookie");
            setCookieHeaders.Should().Contain(h => h.Contains("refreshToken=;"));
        }
    }
}

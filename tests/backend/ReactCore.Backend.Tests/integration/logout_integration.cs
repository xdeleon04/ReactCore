using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ReactCore.Backend.Data;
using ReactCore.Backend.Models;
using Xunit;

namespace ReactCore.Tests.Integration
{
    public class LogoutIntegrationTests : IClassFixture<CustomWebApplicationFactory<Program>>
    {
        private readonly CustomWebApplicationFactory<Program> _factory;

        public LogoutIntegrationTests(CustomWebApplicationFactory<Program> factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task Logout_ShouldInvalidateRefreshToken_InDatabase()
        {
            // Arrange
            var client = _factory.CreateClient();
            var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            // Create a user with a valid refresh token
            var email = $"logout_test_{Guid.NewGuid()}@example.com";
            var user = new User
            {
                Email = email,
                PasswordHash = "hashed_password",
                RefreshToken = "valid_refresh_token",
                RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7)
            };
            db.Users.Add(user);
            await db.SaveChangesAsync();

            // Login to get the cookie (simulated by manually adding cookie to request if needed,
            // but for logout we just need to hit the endpoint.
            // Wait, the logout endpoint needs to know WHICH user to logout.
            // Usually logout relies on the RefreshToken cookie to identify the session/user
            // OR the Access Token in the header.
            // If we use the cookie approach for refresh tokens, the logout endpoint should probably
            // take the refresh token from the cookie and revoke it.

            // So we need to send the request with the cookie.
            var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/logout");
            request.Headers.Add("Cookie", "refreshToken=valid_refresh_token");

            // Act
            var response = await client.SendAsync(request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            // Verify token is revoked in DB using a new scope to bypass EF cache
            using var assertScope = _factory.Services.CreateScope();
            var assertDb = assertScope.ServiceProvider.GetRequiredService<AppDbContext>();
            var dbUser = await assertDb.Users.FirstOrDefaultAsync(u => u.Email == email);
            dbUser.Should().NotBeNull();
            dbUser.RefreshToken.Should().BeNull();
            dbUser.RefreshTokenExpiryTime.Should().BeNull();

            // Verify that using the revoked token to refresh fails
            var refreshRequest = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh");
            refreshRequest.Headers.Add("Cookie", "refreshToken=valid_refresh_token");
            var refreshResponse = await client.SendAsync(refreshRequest);
            refreshResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }
    }
}

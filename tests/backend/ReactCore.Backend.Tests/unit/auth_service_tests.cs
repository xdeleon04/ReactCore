using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Moq;
using ReactCore.Backend.Models;
using ReactCore.Backend.Repositories;
using ReactCore.Backend.Services;
using Xunit;

namespace ReactCore.Tests.Unit
{
    public class AuthServiceTests
    {
        private readonly Mock<IUserRepository> _mockUserRepo;
        private readonly Mock<IPasswordValidator> _mockPasswordValidator;
        private readonly Mock<IRateLimitService> _mockRateLimitService;
        private readonly Mock<IConfiguration> _mockConfig;
        private readonly AuthService _authService;

        public AuthServiceTests()
        {
            _mockUserRepo = new Mock<IUserRepository>();
            _mockPasswordValidator = new Mock<IPasswordValidator>();
            _mockRateLimitService = new Mock<IRateLimitService>();
            _mockConfig = new Mock<IConfiguration>();

            // Setup config for JWT
            _mockConfig.Setup(c => c["Jwt:Key"]).Returns("super_secret_key_for_testing_purposes_only_must_be_long");
            _mockConfig.Setup(c => c["Jwt:Issuer"]).Returns("test_issuer");
            _mockConfig.Setup(c => c["Jwt:Audience"]).Returns("test_audience");

            _authService = new AuthService(
                _mockUserRepo.Object,
                _mockPasswordValidator.Object,
                _mockRateLimitService.Object,
                _mockConfig.Object
            );
        }

        [Fact]
        public async Task LoginAsync_ShouldVerifyPasswordCorrectly()
        {
            // Arrange
            var email = "test@example.com";
            var password = "Password123!";
            var hash = BCrypt.Net.BCrypt.HashPassword(password);

            var user = new User
            {
                Email = email,
                PasswordHash = hash
            };

            _mockUserRepo.Setup(r => r.FindByEmailAsync(email)).ReturnsAsync(user);
            _mockRateLimitService.Setup(s => s.IsLimitExceededAsync(email)).ReturnsAsync(false);

            // Act
            var result = await _authService.LoginAsync(email, password, "127.0.0.1");

            // Assert
            result.Should().NotBeNull();
        }

        [Fact]
        public async Task LoginAsync_ShouldFail_WhenPasswordIsIncorrect()
        {
            // Arrange
            var email = "test@example.com";
            var password = "Password123!";
            var hash = BCrypt.Net.BCrypt.HashPassword(password);

            var user = new User
            {
                Email = email,
                PasswordHash = hash
            };

            _mockUserRepo.Setup(r => r.FindByEmailAsync(email)).ReturnsAsync(user);
            _mockRateLimitService.Setup(s => s.IsLimitExceededAsync(email)).ReturnsAsync(false);

            // Act
            var result = await _authService.LoginAsync(email, "WrongPassword", "127.0.0.1");

            // Assert
            result.Should().BeNull();
        }
    }
}

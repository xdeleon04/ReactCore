using FluentAssertions;
using ReactCore.Backend.Services;
using Xunit;

namespace ReactCore.Tests.Unit
{
    public class PasswordValidatorTests
    {
        private readonly PasswordValidator _validator;

        public PasswordValidatorTests()
        {
            _validator = new PasswordValidator();
        }

        [Theory]
        [InlineData("Password123!", true)]
        [InlineData("short", false)]
        [InlineData("nouppercase1!", false)]
        [InlineData("NOLOWERCASE1!", false)]
        [InlineData("NoNumber!", false)] // Assuming number is required, let's check implementation
        [InlineData("NoSpecialChar1", false)] // Assuming special char is required
        public void ValidatePassword_ShouldReturnExpectedResult(string password, bool expected)
        {
            // Act
            var result = _validator.ValidatePassword(password);

            // Assert
            result.Should().Be(expected);
        }
    }
}

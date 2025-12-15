using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using ReactCore.Backend.Models;
using ReactCore.Backend.Repositories;

namespace ReactCore.Backend.Services;

public interface IAuthService
{
    Task<(string AccessToken, string RefreshToken)?> LoginAsync(string email, string password, string? ipAddress);
    Task<(string AccessToken, string RefreshToken)> GenerateTokensAsync(User user);
    Task<(string AccessToken, string RefreshToken)?> RefreshTokenAsync(string refreshToken);
    Task RevokeTokenAsync(string refreshToken);
}

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordValidator _passwordValidator;
    private readonly IRateLimitService _rateLimitService;
    private readonly IConfiguration _configuration;

    public AuthService(
        IUserRepository userRepository,
        IPasswordValidator passwordValidator,
        IRateLimitService rateLimitService,
        IConfiguration configuration)
    {
        _userRepository = userRepository;
        _passwordValidator = passwordValidator;
        _rateLimitService = rateLimitService;
        _configuration = configuration;
    }

    // TODO: For future Registration method:
    // 1. Validate password using _passwordValidator.ValidatePassword(password)
    // 2. If invalid, return error with _passwordValidator.GetStrengthFeedback(password)
    // 3. Hash password using BCrypt.Net.BCrypt.HashPassword(password)
    // 4. Save user with hashed password
    public async Task<(string AccessToken, string RefreshToken)?> LoginAsync(string email, string password, string? ipAddress)
    {
        if (await _rateLimitService.IsLimitExceededAsync(email))
        {
            await _rateLimitService.TrackAttemptAsync(email, false, ipAddress);
            throw new InvalidOperationException("Too many login attempts. Please try again later.");
        }

        var user = await _userRepository.FindByEmailAsync(email);
        if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
        {
            await _rateLimitService.TrackAttemptAsync(email, false, ipAddress);
            return null;
        }

        if (!user.IsActive)
        {
            await _rateLimitService.TrackAttemptAsync(email, false, ipAddress);
            throw new InvalidOperationException("Account has been deactivated.");
        }

        if (user.IsLocked)
        {
            await _rateLimitService.TrackAttemptAsync(email, false, ipAddress);
            throw new InvalidOperationException("Account is locked.");
        }

        await _rateLimitService.TrackAttemptAsync(email, true, ipAddress);

        var tokens = await GenerateTokensAsync(user);

        // Save refresh token
        user.RefreshToken = tokens.RefreshToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
        await _userRepository.UpdateAsync(user);

        return tokens;
    }

    public async Task<(string AccessToken, string RefreshToken)> GenerateTokensAsync(User user)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role)
            }),
            Expires = DateTime.UtcNow.AddMinutes(15),
            Issuer = _configuration["Jwt:Issuer"],
            Audience = _configuration["Jwt:Audience"],
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var accessToken = tokenHandler.WriteToken(tokenHandler.CreateToken(tokenDescriptor));
        var refreshToken = Convert.ToBase64String(Guid.NewGuid().ToByteArray());

        return await Task.FromResult((accessToken, refreshToken));
    }

    public async Task<(string AccessToken, string RefreshToken)?> RefreshTokenAsync(string refreshToken)
    {
        var user = await _userRepository.FindByRefreshTokenAsync(refreshToken);
        if (user == null || user.RefreshToken != refreshToken || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
        {
            return null;
        }

        var tokens = await GenerateTokensAsync(user);

        // Rotate refresh token
        user.RefreshToken = tokens.RefreshToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
        await _userRepository.UpdateAsync(user);

        return tokens;
    }

    public async Task RevokeTokenAsync(string refreshToken)
    {
        var user = await _userRepository.FindByRefreshTokenAsync(refreshToken);
        if (user != null)
        {
            user.RefreshToken = null;
            user.RefreshTokenExpiryTime = null;
            await _userRepository.UpdateAsync(user);
        }
    }
}

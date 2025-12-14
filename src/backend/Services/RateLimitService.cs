using Microsoft.EntityFrameworkCore;
using ReactCore.Backend.Data;
using ReactCore.Backend.Models;

namespace ReactCore.Backend.Services;

public interface IRateLimitService
{
    Task TrackAttemptAsync(string email, bool isSuccessful, string? ipAddress);
    Task<bool> IsLimitExceededAsync(string email);
}

public class RateLimitService : IRateLimitService
{
    private readonly AppDbContext _context;
    private const int MaxAttempts = 5;
    private const int WindowMinutes = 15;

    public RateLimitService(AppDbContext context)
    {
        _context = context;
    }

    public async Task TrackAttemptAsync(string email, bool isSuccessful, string? ipAddress)
    {
        var attempt = new LoginAttempt
        {
            Email = email,
            IsSuccessful = isSuccessful,
            IpAddress = ipAddress,
            Timestamp = DateTime.UtcNow
        };

        _context.LoginAttempts.Add(attempt);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> IsLimitExceededAsync(string email)
    {
        var windowStart = DateTime.UtcNow.AddMinutes(-WindowMinutes);

        var failedAttempts = await _context.LoginAttempts
            .Where(l => l.Email == email && !l.IsSuccessful && l.Timestamp >= windowStart)
            .CountAsync();

        return failedAttempts >= MaxAttempts;
    }
}

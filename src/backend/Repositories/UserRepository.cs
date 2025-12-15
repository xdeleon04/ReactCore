using Microsoft.EntityFrameworkCore;
using ReactCore.Backend.Models;

namespace ReactCore.Backend.Repositories;

public interface IUserRepository
{
    Task<User?> FindByEmailAsync(string email);
    Task<User?> FindByRefreshTokenAsync(string refreshToken);
    Task<User?> GetByIdAsync(Guid id);
    IQueryable<User> QueryForAdmin(string? email, string? role, bool? isActive);
    Task CreateAsync(User user);
    Task UpdateAsync(User user);
}

public class UserRepository : IUserRepository
{
    private readonly Data.AppDbContext _context;

    public UserRepository(Data.AppDbContext context)
    {
        _context = context;
    }

    public async Task<User?> FindByEmailAsync(string email)
    {
        return await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(_context.Users, u => u.Email == email);
    }

    public async Task<User?> FindByRefreshTokenAsync(string refreshToken)
    {
        return await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(_context.Users, u => u.RefreshToken == refreshToken);
    }

    public async Task<User?> GetByIdAsync(Guid id)
    {
        return await _context.Users.FindAsync(id);
    }

    public IQueryable<User> QueryForAdmin(string? email, string? role, bool? isActive)
    {
        var query = _context.Users.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(email))
        {
            var needle = email.Trim().ToLowerInvariant();
            query = query.Where(u => u.Email.ToLower().Contains(needle));
        }

        if (!string.IsNullOrWhiteSpace(role))
        {
            var normalizedRole = role.Trim().ToLowerInvariant();
            query = query.Where(u => u.Role.ToLower() == normalizedRole);
        }

        if (isActive.HasValue)
        {
            query = query.Where(u => u.IsActive == isActive.Value);
        }

        return query;
    }

    public async Task CreateAsync(User user)
    {
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(User user)
    {
        _context.Users.Update(user);
        await _context.SaveChangesAsync();
    }
}

using System.Security.Claims;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ReactCore.Backend.Data;
using ReactCore.Backend.Models;
using ReactCore.Backend.Models.Dto;
using ReactCore.Backend.Models.Enums;
using ReactCore.Backend.Repositories;

namespace ReactCore.Backend.Services.Admin;

public interface IAdminUserService
{
    Task<(List<UserListDto> Items, int Total)> ListUsersAsync(string? email, string? role, bool? isActive, int skip, int take, CancellationToken cancellationToken);
    Task<AdminUserDetailDto?> GetUserDetailAsync(Guid userId, CancellationToken cancellationToken);
    Task<(bool Success, string? Error, DateTime Timestamp)> DeactivateAsync(Guid adminUserId, Guid userId, string? reason, string? ipAddress, CancellationToken cancellationToken);
    Task<(bool Success, string? Error, DateTime Timestamp)> ReactivateAsync(Guid adminUserId, Guid userId, string? ipAddress, CancellationToken cancellationToken);
}

public class AdminUserService : IAdminUserService
{
    private readonly AppDbContext _db;
    private readonly IUserRepository _users;

    public AdminUserService(AppDbContext db, IUserRepository users)
    {
        _db = db;
        _users = users;
    }

    public async Task<(List<UserListDto> Items, int Total)> ListUsersAsync(string? email, string? role, bool? isActive, int skip, int take, CancellationToken cancellationToken)
    {
        take = Math.Clamp(take, 1, 100);
        skip = Math.Max(0, skip);

        var query = _users.QueryForAdmin(email, role, isActive);

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(u => u.Email)
            .Skip(skip)
            .Take(take)
            .Select(u => new UserListDto
            {
                Id = u.Id,
                Email = u.Email,
                Role = u.Role,
                IsActive = u.IsActive,
                CreatedAt = u.CreatedAt,
                LastLogin = _db.LoginAttempts
                    .Where(l => l.Email == u.Email && l.IsSuccessful)
                    .OrderByDescending(l => l.Timestamp)
                    .Select(l => (DateTime?)l.Timestamp)
                    .FirstOrDefault(),
            })
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public async Task<AdminUserDetailDto?> GetUserDetailAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user is null)
        {
            return null;
        }

        var lastLogin = await _db.LoginAttempts
            .AsNoTracking()
            .Where(l => l.Email == user.Email && l.IsSuccessful)
            .OrderByDescending(l => l.Timestamp)
            .Select(l => (DateTime?)l.Timestamp)
            .FirstOrDefaultAsync(cancellationToken);

        var orderHistory = await _db.Orders
            .AsNoTracking()
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.CreatedAt)
            .Take(50)
            .Select(o => new AdminUserOrderHistoryDto
            {
                OrderNumber = o.OrderNumber,
                Date = o.CreatedAt,
                Total = o.TotalPrice,
                Status = o.Status,
            })
            .ToListAsync(cancellationToken);

        var cart = await _db.Carts
            .AsNoTracking()
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);

        var cartItemCount = cart?.Items.Sum(i => i.Quantity) ?? 0;

        return new AdminUserDetailDto
        {
            Id = user.Id,
            Email = user.Email,
            Role = user.Role,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt,
            LastLogin = lastLogin,
            OrderHistory = orderHistory,
            CartActivity = new AdminUserCartActivityDto
            {
                ItemCount = cartItemCount,
                LastUpdated = cart?.UpdatedAt,
            }
        };
    }

    public async Task<(bool Success, string? Error, DateTime Timestamp)> DeactivateAsync(Guid adminUserId, Guid userId, string? reason, string? ipAddress, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user is null)
        {
            return (false, "User not found", now);
        }

        if (!user.IsActive)
        {
            return (false, "User is already inactive", now);
        }

        var oldValues = JsonSerializer.Serialize(new { isActive = true });
        var newValues = JsonSerializer.Serialize(new { isActive = false });

        user.IsActive = false;
        user.UpdatedAt = now;

        _db.AdminActions.Add(new AdminAction
        {
            AdminUserId = adminUserId,
            ActionType = AdminActionType.UserDeactivate.ToString(),
            EntityType = EntityType.User.ToString(),
            EntityId = userId.ToString(),
            OldValues = oldValues,
            NewValues = newValues,
            Timestamp = now,
            IpAddress = ipAddress,
            Reason = reason,
        });

        await _db.SaveChangesAsync(cancellationToken);

        return (true, null, now);
    }

    public async Task<(bool Success, string? Error, DateTime Timestamp)> ReactivateAsync(Guid adminUserId, Guid userId, string? ipAddress, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user is null)
        {
            return (false, "User not found", now);
        }

        if (user.IsActive)
        {
            return (false, "User is already active", now);
        }

        var oldValues = JsonSerializer.Serialize(new { isActive = false });
        var newValues = JsonSerializer.Serialize(new { isActive = true });

        user.IsActive = true;
        user.UpdatedAt = now;

        _db.AdminActions.Add(new AdminAction
        {
            AdminUserId = adminUserId,
            ActionType = AdminActionType.UserReactivate.ToString(),
            EntityType = EntityType.User.ToString(),
            EntityId = userId.ToString(),
            OldValues = oldValues,
            NewValues = newValues,
            Timestamp = now,
            IpAddress = ipAddress,
        });

        await _db.SaveChangesAsync(cancellationToken);

        return (true, null, now);
    }
}

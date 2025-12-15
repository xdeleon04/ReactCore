using Microsoft.EntityFrameworkCore;
using ReactCore.Backend.Data;
using ReactCore.Backend.Models;
using ReactCore.Backend.Models.Dto;

namespace ReactCore.Backend.Services.Admin;

public interface IAdminAuditLogService
{
    Task<(IReadOnlyList<AdminAuditLogDto> Items, int Total)> ListAsync(DateTime? startDate, DateTime? endDate, int skip, int take, CancellationToken cancellationToken = default);
    Task<AdminAuditLogDetailDto?> GetDetailAsync(int auditLogId, CancellationToken cancellationToken = default);
}

public class AdminAuditLogService : IAdminAuditLogService
{
    private readonly AppDbContext _db;

    public AdminAuditLogService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<(IReadOnlyList<AdminAuditLogDto> Items, int Total)> ListAsync(
        DateTime? startDate,
        DateTime? endDate,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var safeSkip = Math.Max(0, skip);
        var safeTake = Math.Clamp(take, 1, 100);

        IQueryable<AdminAction> query = _db.AdminActions.AsNoTracking().Include(a => a.Admin);

        if (startDate.HasValue)
        {
            var startUtc = startDate.Value.Kind == DateTimeKind.Utc ? startDate.Value : startDate.Value.ToUniversalTime();
            query = query.Where(a => a.Timestamp >= startUtc);
        }

        if (endDate.HasValue)
        {
            var endUtc = endDate.Value.Kind == DateTimeKind.Utc ? endDate.Value : endDate.Value.ToUniversalTime();
            query = query.Where(a => a.Timestamp <= endUtc);
        }

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(a => a.Timestamp)
            .ThenByDescending(a => a.Id)
            .Skip(safeSkip)
            .Take(safeTake)
            .Select(a => new AdminAuditLogDto
            {
                Id = a.Id,
                AdminEmail = a.Admin != null ? a.Admin.Email : string.Empty,
                Action = a.ActionType,
                EntityType = a.EntityType,
                EntityId = a.EntityId,
                Timestamp = a.Timestamp,
                OldValues = a.OldValues,
                NewValues = a.NewValues,
                Reason = a.Reason,
            })
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public async Task<AdminAuditLogDetailDto?> GetDetailAsync(int auditLogId, CancellationToken cancellationToken = default)
    {
        return await _db.AdminActions
            .AsNoTracking()
            .Include(a => a.Admin)
            .Where(a => a.Id == auditLogId)
            .Select(a => new AdminAuditLogDetailDto
            {
                Id = a.Id,
                AdminId = a.AdminUserId,
                AdminEmail = a.Admin != null ? a.Admin.Email : string.Empty,
                Action = a.ActionType,
                EntityType = a.EntityType,
                EntityId = a.EntityId,
                OldValues = a.OldValues,
                NewValues = a.NewValues,
                Timestamp = a.Timestamp,
                IpAddress = a.IpAddress,
                Reason = a.Reason,
            })
            .FirstOrDefaultAsync(cancellationToken);
    }
}

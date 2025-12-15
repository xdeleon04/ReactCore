using Microsoft.EntityFrameworkCore;
using ReactCore.Backend.Data;
using ReactCore.Backend.Models.Dto;

namespace ReactCore.Backend.Services.Admin;

public interface IAdminDashboardService
{
    Task<DashboardSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default);
}

public class AdminDashboardService : IAdminDashboardService
{
    private readonly AppDbContext _db;

    public AdminDashboardService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<DashboardSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        var utcNow = DateTime.UtcNow;
        var monthStart = new DateTime(utcNow.Year, utcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var todayStart = utcNow.Date;
        var tomorrowStart = todayStart.AddDays(1);

        // DbContext is not thread-safe; avoid running multiple queries concurrently.
        var totalUsers = await _db.Users.AsNoTracking().CountAsync(cancellationToken);
        var activeUsers = await _db.Users.AsNoTracking().CountAsync(u => u.IsActive, cancellationToken);
        var inactiveUsers = await _db.Users.AsNoTracking().CountAsync(u => !u.IsActive, cancellationToken);
        var newUsersThisMonth = await _db.Users.AsNoTracking().CountAsync(u => u.CreatedAt >= monthStart, cancellationToken);

        var totalProducts = await _db.Products.AsNoTracking().CountAsync(cancellationToken);
        var activeProducts = await _db.Products.AsNoTracking().CountAsync(p => !p.IsDeleted, cancellationToken);
        var archivedProducts = await _db.Products.AsNoTracking().CountAsync(p => p.IsDeleted, cancellationToken);
        var lowStockCount = await _db.Products.AsNoTracking().CountAsync(p => !p.IsDeleted && p.StockQuantity <= p.ReorderLevel, cancellationToken);

        var pendingOrders = await _db.Orders.AsNoTracking().CountAsync(o => o.Status == "Pending", cancellationToken);
        var processingOrders = await _db.Orders.AsNoTracking().CountAsync(o => o.Status == "Processing", cancellationToken);
        var totalOrdersToday = await _db.Orders.AsNoTracking().CountAsync(o => o.CreatedAt >= todayStart && o.CreatedAt < tomorrowStart, cancellationToken);
        var todayRevenue = await _db.Orders
            .AsNoTracking()
            .Where(o => o.CreatedAt >= todayStart && o.CreatedAt < tomorrowStart)
            .Select(o => (decimal?)o.TotalPrice)
            .SumAsync(cancellationToken);

        return new DashboardSummaryDto
        {
            UserMetrics = new UserMetricsDto
            {
                TotalUsers = totalUsers,
                ActiveUsers = activeUsers,
                InactiveUsers = inactiveUsers,
                NewUsersThisMonth = newUsersThisMonth,
            },
            ProductMetrics = new ProductMetricsDto
            {
                TotalProducts = totalProducts,
                ActiveProducts = activeProducts,
                ArchivedProducts = archivedProducts,
                LowStockCount = lowStockCount,
            },
            OrderMetrics = new OrderMetricsDto
            {
                PendingOrders = pendingOrders,
                ProcessingOrders = processingOrders,
                TotalOrdersToday = totalOrdersToday,
                TodayRevenue = todayRevenue ?? 0m,
            }
        };
    }
}

using Microsoft.EntityFrameworkCore;
using ReactCore.Backend.Data;
using ReactCore.Backend.Models;

namespace ReactCore.Backend.Services;

public class NotificationService : INotificationService
{
    private const string NotificationTypeOutOfStock = "OutOfStockNotification";

    private readonly AppDbContext _context;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(AppDbContext context, ILogger<NotificationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task SubscribeToRestockAsync(Guid userId, int productId, CancellationToken cancellationToken = default)
    {
        if (productId <= 0) throw new ArgumentOutOfRangeException(nameof(productId), "productId must be >= 1");

        var exists = await _context.Products
            .AsNoTracking()
            .AnyAsync(p => p.Id == productId && !p.IsDeleted, cancellationToken);

        if (!exists)
        {
            throw new KeyNotFoundException("Product not found");
        }

        var existing = await _context.NotificationPreferences
            .FirstOrDefaultAsync(
                np => np.UserId == userId
                      && np.ProductId == productId
                      && np.NotificationType == NotificationTypeOutOfStock,
                cancellationToken);

        if (existing is not null)
        {
            return;
        }

        _context.NotificationPreferences.Add(new NotificationPreferences
        {
            UserId = userId,
            ProductId = productId,
            NotificationType = NotificationTypeOutOfStock,
            CreatedAt = DateTime.UtcNow,
            NotifiedAt = null
        });

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Subscribed to restock notifications userId={UserId} productId={ProductId}", userId, productId);
    }

    public async Task<IReadOnlyList<TriggeredNotificationDto>> CheckPendingNotificationsAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        var pending = await _context.NotificationPreferences
            .Include(np => np.Product)
            .Where(np => np.UserId == userId
                         && np.NotificationType == NotificationTypeOutOfStock
                         && np.NotifiedAt == null
                         && np.Product != null
                         && !np.Product.IsDeleted
                         && np.Product.StockQuantity > 0)
            .ToListAsync(cancellationToken);

        if (pending.Count == 0)
        {
            return Array.Empty<TriggeredNotificationDto>();
        }

        foreach (var np in pending)
        {
            np.NotifiedAt = now;
        }

        await _context.SaveChangesAsync(cancellationToken);

        var results = pending
            .Select(np => new TriggeredNotificationDto(
                ProductId: np.ProductId,
                ProductName: np.Product?.Name ?? string.Empty,
                StockQuantity: np.Product?.StockQuantity ?? 0,
                ReorderLevel: np.Product?.ReorderLevel ?? 0,
                Status: GetStatus(np.Product?.StockQuantity ?? 0, np.Product?.ReorderLevel ?? 0),
                TriggeredAt: now))
            .ToList();

        _logger.LogInformation("Triggered {Count} restock notifications userId={UserId}", results.Count, userId);

        return results;
    }

    private static string GetStatus(int stockQuantity, int reorderLevel)
    {
        if (stockQuantity <= 0) return "out-of-stock";
        if (stockQuantity <= reorderLevel) return "low-stock";
        return "in-stock";
    }
}

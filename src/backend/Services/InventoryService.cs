using Microsoft.EntityFrameworkCore;
using ReactCore.Backend.Data;
using ReactCore.Backend.Models;

namespace ReactCore.Backend.Services;

public class InventoryService : IInventoryService
{
    private readonly AppDbContext _context;

    public InventoryService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<int> CheckStockAsync(int productId, CancellationToken cancellationToken = default)
    {
        return await _context.Products
            .Where(p => p.Id == productId && !p.IsDeleted)
            .Select(p => p.StockQuantity)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<Product?> GetProductForUpdateAsync(int productId, CancellationToken cancellationToken = default)
    {
        if (_context.Database.IsSqlServer())
        {
            return _context.Products
                .FromSqlInterpolated($"SELECT * FROM Products WITH (UPDLOCK, ROWLOCK, HOLDLOCK) WHERE Id = {productId}")
                .FirstOrDefaultAsync(cancellationToken);
        }

        return _context.Products
            .FirstOrDefaultAsync(p => p.Id == productId, cancellationToken);
    }

    public async Task<(int PreviousQuantity, int NewQuantity)> ReserveStockAsync(Product product, int quantity, int? orderId, CancellationToken cancellationToken = default)
    {
        var previous = product.StockQuantity;
        product.StockQuantity = Math.Max(0, product.StockQuantity - quantity);

        _context.InventoryAudits.Add(new InventoryAudit
        {
            ProductId = product.Id,
            PreviousQuantity = previous,
            NewQuantity = product.StockQuantity,
            Reason = "ORDER_PLACED",
            OrderId = orderId,
            Timestamp = DateTime.UtcNow
        });

        await _context.SaveChangesAsync(cancellationToken);
        return (previous, product.StockQuantity);
    }

    public async Task<(int PreviousQuantity, int NewQuantity)> ReleaseStockAsync(Product product, int quantity, int? orderId, CancellationToken cancellationToken = default)
    {
        var previous = product.StockQuantity;
        product.StockQuantity = product.StockQuantity + quantity;

        _context.InventoryAudits.Add(new InventoryAudit
        {
            ProductId = product.Id,
            PreviousQuantity = previous,
            NewQuantity = product.StockQuantity,
            Reason = "ORDER_RELEASED",
            OrderId = orderId,
            Timestamp = DateTime.UtcNow
        });

        await _context.SaveChangesAsync(cancellationToken);
        return (previous, product.StockQuantity);
    }
}

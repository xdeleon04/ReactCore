using ReactCore.Backend.Models;

namespace ReactCore.Backend.Services;

public interface IInventoryService
{
    Task<int> CheckStockAsync(int productId, CancellationToken cancellationToken = default);
    Task<Product?> GetProductForUpdateAsync(int productId, CancellationToken cancellationToken = default);
    Task<(int PreviousQuantity, int NewQuantity)> ReserveStockAsync(Product product, int quantity, int? orderId, CancellationToken cancellationToken = default);
    Task<(int PreviousQuantity, int NewQuantity)> ReleaseStockAsync(Product product, int quantity, int? orderId, CancellationToken cancellationToken = default);
}

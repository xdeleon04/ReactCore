using ReactCore.Backend.Models;

namespace ReactCore.Backend.Repositories;

public interface ICartRepository
{
    Task<Cart> GetOrCreateAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<Cart?> GetCurrentAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<CartItem?> GetItemAsync(Guid userId, int itemId, CancellationToken cancellationToken = default);
    void RemoveItem(CartItem item);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

using Microsoft.EntityFrameworkCore;
using ReactCore.Backend.Data;
using ReactCore.Backend.Models;

namespace ReactCore.Backend.Repositories;

public class CartRepository : ICartRepository
{
    private readonly AppDbContext _context;

    public CartRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Cart> GetOrCreateAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var existing = await _context.Carts
            .Include(c => c.Items)
            .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);

        if (existing is not null)
        {
            return existing;
        }

        var cart = new Cart
        {
            UserId = userId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Carts.Add(cart);
        await _context.SaveChangesAsync(cancellationToken);
        return cart;
    }

    public Task<Cart?> GetCurrentAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return _context.Carts
            .AsNoTracking()
            .Include(c => c.Items)
            .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);
    }

    public Task<CartItem?> GetItemAsync(Guid userId, int itemId, CancellationToken cancellationToken = default)
    {
        return _context.CartItems
            .Include(ci => ci.Cart)
            .Include(ci => ci.Product)
            .FirstOrDefaultAsync(ci => ci.Id == itemId && ci.Cart != null && ci.Cart.UserId == userId, cancellationToken);
    }

    public void RemoveItem(CartItem item)
    {
        _context.CartItems.Remove(item);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _context.SaveChangesAsync(cancellationToken);
    }
}

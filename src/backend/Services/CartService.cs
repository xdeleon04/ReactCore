using ReactCore.Backend.Models;
using ReactCore.Backend.Models.Dto;
using ReactCore.Backend.Repositories;

namespace ReactCore.Backend.Services;

public class CartService : ICartService
{
    private readonly ICartRepository _cartRepository;
    private readonly IProductRepository _productRepository;
    private readonly ILogger<CartService> _logger;

    public CartService(ICartRepository cartRepository, IProductRepository productRepository, ILogger<CartService> logger)
    {
        _cartRepository = cartRepository;
        _productRepository = productRepository;
        _logger = logger;
    }

    public async Task<CartDto> GetCartAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var cart = await _cartRepository.GetOrCreateAsync(userId, cancellationToken);
        return MapCart(cart);
    }

    public async Task<AddToCartResponse> AddItemAsync(Guid userId, AddToCartRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Quantity <= 0) throw new ArgumentOutOfRangeException(nameof(request.Quantity), "Quantity must be > 0");
        if (request.ProductId <= 0) throw new ArgumentOutOfRangeException(nameof(request.ProductId), "ProductId must be > 0");

        var product = await _productRepository.GetByIdAsync(request.ProductId, cancellationToken);
        if (product is null)
        {
            throw new ProductNotFoundException(request.ProductId);
        }

        var cart = await _cartRepository.GetOrCreateAsync(userId, cancellationToken);
        var existing = cart.Items.FirstOrDefault(i => i.ProductId == request.ProductId);
        var nextQuantity = (existing?.Quantity ?? 0) + request.Quantity;

        ValidateAgainstStock(product, nextQuantity);

        if (existing is null)
        {
            existing = new CartItem
            {
                ProductId = product.Id,
                Product = product,
                Quantity = nextQuantity,
                UnitPrice = product.Price,
                AddedAt = DateTime.UtcNow
            };
            cart.Items.Add(existing);
        }
        else
        {
            existing.Quantity = nextQuantity;
            existing.UnitPrice = product.Price;
        }

        cart.UpdatedAt = DateTime.UtcNow;
        await _cartRepository.SaveChangesAsync(cancellationToken);

        var mapped = MapCart(cart);

        _logger.LogInformation(
            "Added item to cart userId={UserId} productId={ProductId} quantity={Quantity} itemCount={ItemCount}",
            userId,
            product.Id,
            request.Quantity,
            mapped.ItemCount);

        return new AddToCartResponse(true, cart.Id, existing.Id, mapped.ItemCount, mapped.Subtotal);
    }

    public async Task<UpdateCartItemResponse> UpdateItemAsync(
        Guid userId,
        int itemId,
        UpdateCartItemRequest request,
        CancellationToken cancellationToken = default)
    {
        if (itemId <= 0) throw new ArgumentOutOfRangeException(nameof(itemId), "itemId must be > 0");
        if (request.Quantity <= 0) throw new ArgumentOutOfRangeException(nameof(request.Quantity), "Quantity must be > 0");

        var cart = await _cartRepository.GetOrCreateAsync(userId, cancellationToken);
        var item = cart.Items.FirstOrDefault(i => i.Id == itemId);
        if (item is null)
        {
            throw new CartItemNotFoundException(itemId);
        }

        var product = item.Product ?? await _productRepository.GetByIdAsync(item.ProductId, cancellationToken);
        if (product is null)
        {
            throw new ProductNotFoundException(item.ProductId);
        }

        ValidateAgainstStock(product, request.Quantity);

        item.Quantity = request.Quantity;
        item.UnitPrice = product.Price;
        cart.UpdatedAt = DateTime.UtcNow;

        await _cartRepository.SaveChangesAsync(cancellationToken);
        var mapped = MapCart(cart);

        return new UpdateCartItemResponse(
            true,
            cart.Id,
            item.Id,
            item.Quantity,
            item.UnitPrice * item.Quantity,
            mapped.Subtotal);
    }

    public async Task RemoveItemAsync(Guid userId, int itemId, CancellationToken cancellationToken = default)
    {
        if (itemId <= 0) throw new ArgumentOutOfRangeException(nameof(itemId), "itemId must be > 0");

        var item = await _cartRepository.GetItemAsync(userId, itemId, cancellationToken);
        if (item is null)
        {
            throw new CartItemNotFoundException(itemId);
        }

        _cartRepository.RemoveItem(item);
        await _cartRepository.SaveChangesAsync(cancellationToken);
    }

    public async Task ClearCartAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var cart = await _cartRepository.GetOrCreateAsync(userId, cancellationToken);
        if (cart.Items.Count == 0) return;

        foreach (var item in cart.Items.ToList())
        {
            _cartRepository.RemoveItem(item);
        }

        cart.Items.Clear();
        cart.UpdatedAt = DateTime.UtcNow;
        await _cartRepository.SaveChangesAsync(cancellationToken);
    }

    private static void ValidateAgainstStock(Product product, int requestedQuantity)
    {
        if (requestedQuantity > product.StockQuantity)
        {
            throw new InsufficientInventoryException(product.Id, requestedQuantity, product.StockQuantity);
        }
    }

    private static CartDto MapCart(Cart cart)
    {
        var items = cart.Items
            .OrderBy(i => i.Id)
            .Select(i => new CartItemDto(
                i.Id,
                i.ProductId,
                i.Product?.Name ?? string.Empty,
                i.Quantity,
                i.UnitPrice,
                i.UnitPrice * i.Quantity,
                i.Product?.ImageUrl))
            .ToList();

        var itemCount = items.Sum(i => i.Quantity);
        var subtotal = items.Sum(i => i.LineTotal);
        var total = subtotal;

        return new CartDto(
            cart.Id,
            cart.UserId,
            items,
            itemCount,
            subtotal,
            total,
            cart.CreatedAt,
            cart.UpdatedAt);
    }
}

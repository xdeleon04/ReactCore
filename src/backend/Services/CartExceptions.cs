namespace ReactCore.Backend.Services;

public sealed class InsufficientInventoryException : Exception
{
    public int ProductId { get; }
    public int RequestedQuantity { get; }
    public int AvailableQuantity { get; }

    public InsufficientInventoryException(int productId, int requestedQuantity, int availableQuantity)
        : base("Insufficient inventory")
    {
        ProductId = productId;
        RequestedQuantity = requestedQuantity;
        AvailableQuantity = availableQuantity;
    }
}

public sealed class CartItemNotFoundException : Exception
{
    public int ItemId { get; }

    public CartItemNotFoundException(int itemId)
        : base("Cart item not found")
    {
        ItemId = itemId;
    }
}

public sealed class ProductNotFoundException : Exception
{
    public int ProductId { get; }

    public ProductNotFoundException(int productId)
        : base("Product not found")
    {
        ProductId = productId;
    }
}

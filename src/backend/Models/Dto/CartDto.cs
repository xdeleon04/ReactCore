namespace ReactCore.Backend.Models.Dto;

public record AddToCartRequest(
    int ProductId,
    int Quantity
);

public record AddToCartResponse(
    bool Success,
    int CartId,
    int CartItemId,
    int ItemCount,
    decimal Subtotal
);

public record UpdateCartItemRequest(
    int Quantity
);

public record UpdateCartItemResponse(
    bool Success,
    int CartId,
    int ItemId,
    int NewQuantity,
    decimal NewLineTotal,
    decimal Subtotal
);

public record CartItemDto(
    int Id,
    int ProductId,
    string ProductName,
    int Quantity,
    decimal UnitPrice,
    decimal LineTotal,
    string? ImageUrl
);

public record CartDto(
    int Id,
    Guid UserId,
    IReadOnlyList<CartItemDto> Items,
    int ItemCount,
    decimal Subtotal,
    decimal Total,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

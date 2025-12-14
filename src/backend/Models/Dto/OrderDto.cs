namespace ReactCore.Backend.Models.Dto;

public record CreateOrderRequest(
    string Email,
    int CartId
);

public record OrderItemDto(
    int Id,
    int ProductId,
    string ProductName,
    int Quantity,
    decimal UnitPrice,
    decimal LineTotal
);

public record OrderDto(
    int Id,
    string OrderNumber,
    Guid UserId,
    string Email,
    decimal Subtotal,
    decimal Total,
    string Status,
    IReadOnlyList<OrderItemDto> Items,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

public record OrderListItemDto(
    int Id,
    string OrderNumber,
    decimal Subtotal,
    decimal Total,
    string Status,
    int ItemCount,
    DateTime CreatedAt
);

public record OrderListDto(
    IReadOnlyList<OrderListItemDto> Items,
    int Total,
    int Skip,
    int Take
);

public record InventoryConflictDto(
    int CartItemId,
    int ProductId,
    string ProductName,
    int RequestedQuantity,
    int AvailableQuantity,
    string Action
);

public record CreateOrderConflictResponse(
    int StatusCode,
    string Message,
    IReadOnlyList<InventoryConflictDto> Conflicts
);

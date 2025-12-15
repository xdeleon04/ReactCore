namespace ReactCore.Backend.Models.Dto;

public record AdminOrderItemDetailDto(
    int ProductId,
    string ProductName,
    int Quantity,
    decimal UnitPrice,
    decimal LineTotal
);

public record AdminOrderDetailDto(
    int Id,
    string OrderNumber,
    string CustomerEmail,
    string Status,
    DateTime CreatedAt,
    IReadOnlyList<AdminOrderItemDetailDto> Items,
    decimal Subtotal,
    decimal Total,
    IReadOnlyList<string> AllowedStatusTransitions
);

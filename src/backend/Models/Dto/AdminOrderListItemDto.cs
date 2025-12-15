namespace ReactCore.Backend.Models.Dto;

public record AdminOrderListItemDto(
    int Id,
    string OrderNumber,
    string CustomerEmail,
    decimal Total,
    string Status,
    DateTime CreatedAt
);

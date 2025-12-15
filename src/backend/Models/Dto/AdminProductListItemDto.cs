namespace ReactCore.Backend.Models.Dto;

public record AdminProductListItemDto(
    int Id,
    string Name,
    decimal Price,
    string Category,
    int StockQuantity,
    int ReorderLevel,
    bool IsDeleted,
    DateTime UpdatedAt
);

namespace ReactCore.Backend.Models.Dto;

public record AdminProductDetailDto(
    int Id,
    string Name,
    string? Description,
    decimal Price,
    string Category,
    string? ImageUrl,
    IReadOnlyList<string> ImageUrls,
    IReadOnlyDictionary<string, object?> Specifications,
    int StockQuantity,
    int ReorderLevel,
    bool IsDeleted,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

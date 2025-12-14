namespace ReactCore.Backend.Models.Dto;

public record ProductListItemDto(
    int Id,
    string Name,
    string? Description,
    decimal Price,
    string Category,
    string? ImageUrl,
    int StockQuantity,
    int ReorderLevel,
    string Status
);

public record ProductListDto(
    IReadOnlyList<ProductListItemDto> Items,
    int Total,
    int Skip,
    int Take
);

public record RelatedProductDto(
    int Id,
    string Name,
    decimal Price,
    string? ImageUrl,
    string Category
);

public record ProductDetailDto(
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
    string Status,
    IReadOnlyList<RelatedProductDto> RelatedProducts,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public record InventoryStatusResponse(
    int ProductId,
    int StockQuantity,
    string Status,
    int ReorderLevel,
    DateTime LastUpdated
);

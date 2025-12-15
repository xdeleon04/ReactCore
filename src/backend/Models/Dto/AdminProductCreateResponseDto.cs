namespace ReactCore.Backend.Models.Dto;

public record AdminProductCreateResponseDto(
    int Id,
    string Name,
    decimal Price,
    string Category,
    int StockQuantity,
    DateTime CreatedAt
);

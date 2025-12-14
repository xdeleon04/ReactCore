namespace ReactCore.Backend.Models.Dto;

public record InventoryDto(
    int ProductId,
    int StockQuantity,
    int ReorderLevel
);

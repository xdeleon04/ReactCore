namespace ReactCore.Backend.Models.Dto;

public class AdminProductUpsertRequest
{
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public int StockQuantity { get; set; }
    public int? ReorderLevel { get; set; }
    public object? Specifications { get; set; }
    public string? ImageUrl { get; set; }
}

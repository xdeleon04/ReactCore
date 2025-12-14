using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ReactCore.Backend.Models;

public class Product
{
    public int Id { get; set; }

    [Required]
    [MaxLength(255)]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal Price { get; set; }

    [Required]
    [MaxLength(100)]
    public string Category { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? ImageUrl { get; set; }

    public string? Specifications { get; set; }

    public int StockQuantity { get; set; }

    public int ReorderLevel { get; set; } = 5;

    public bool IsDeleted { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public List<ProductImage> Images { get; set; } = new();

    public List<OrderItem> OrderItems { get; set; } = new();

    public List<InventoryAudit> InventoryAudits { get; set; } = new();
}

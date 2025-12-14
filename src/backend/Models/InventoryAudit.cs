using System.ComponentModel.DataAnnotations;

namespace ReactCore.Backend.Models;

public class InventoryAudit
{
    public int Id { get; set; }

    public int ProductId { get; set; }

    public Product? Product { get; set; }

    public int PreviousQuantity { get; set; }

    public int NewQuantity { get; set; }

    [Required]
    [MaxLength(100)]
    public string Reason { get; set; } = string.Empty;

    public int? OrderId { get; set; }

    public Order? Order { get; set; }

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

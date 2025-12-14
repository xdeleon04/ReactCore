using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ReactCore.Backend.Models;

public class Order
{
    public int Id { get; set; }

    [Required]
    public Guid UserId { get; set; }

    public User? User { get; set; }

    [Required]
    [MaxLength(50)]
    public string OrderNumber { get; set; } = string.Empty;

    [Column(TypeName = "decimal(10,2)")]
    public decimal SubtotalPrice { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal TotalPrice { get; set; }

    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = "Pending";

    [Required]
    [MaxLength(255)]
    public string Email { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public List<OrderItem> Items { get; set; } = new();

    public List<InventoryAudit> InventoryAudits { get; set; } = new();
}

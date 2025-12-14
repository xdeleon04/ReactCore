using System.ComponentModel.DataAnnotations;

namespace ReactCore.Backend.Models;

public class Cart
{
    public int Id { get; set; }

    [Required]
    public Guid UserId { get; set; }

    public User? User { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ExpiryTime { get; set; }

    public List<CartItem> Items { get; set; } = new();
}

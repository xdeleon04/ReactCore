using System.ComponentModel.DataAnnotations;

namespace ReactCore.Backend.Models;

public class NotificationPreferences
{
    public int Id { get; set; }

    public Guid? UserId { get; set; }

    public User? User { get; set; }

    public int ProductId { get; set; }

    public Product? Product { get; set; }

    [Required]
    [MaxLength(50)]
    public string NotificationType { get; set; } = "OutOfStockNotification";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? NotifiedAt { get; set; }
}

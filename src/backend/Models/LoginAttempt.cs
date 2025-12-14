using System.ComponentModel.DataAnnotations;

namespace ReactCore.Backend.Models;

public class LoginAttempt
{
    public int Id { get; set; }

    [Required]
    [MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public bool IsSuccessful { get; set; }

    [MaxLength(45)]
    public string? IpAddress { get; set; }
}

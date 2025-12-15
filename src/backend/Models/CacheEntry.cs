using System.ComponentModel.DataAnnotations;

namespace ReactCore.Backend.Models;

public class CacheEntry
{
    public int Id { get; set; }

    [Required]
    [MaxLength(64)]
    public string ApiName { get; set; } = string.Empty;

    [Required]
    [MaxLength(64)]
    public string DataType { get; set; } = string.Empty;

    [Required]
    [MaxLength(256)]
    public string CacheKey { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset ExpiresAt { get; set; }

    public int Hits { get; set; }

    public string? DataJson { get; set; }
}

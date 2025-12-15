using System.ComponentModel.DataAnnotations;

namespace ReactCore.Backend.Models;

public class ExternalApiCall
{
    public int Id { get; set; }

    [Required]
    [MaxLength(64)]
    public string ApiName { get; set; } = string.Empty;

    [Required]
    [MaxLength(256)]
    public string Endpoint { get; set; } = string.Empty;

    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;

    public int StatusCode { get; set; }

    public long ResponseTimeMs { get; set; }

    public bool WasCached { get; set; }

    [MaxLength(2000)]
    public string? Error { get; set; }

    public Guid? UserId { get; set; }
}

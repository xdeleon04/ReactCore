using System.ComponentModel.DataAnnotations;

namespace ReactCore.Backend.Models;

public class ApiQuotaUsage
{
    public int Id { get; set; }

    [Required]
    [MaxLength(64)]
    public string ApiName { get; set; } = string.Empty;

    public DateTimeOffset PeriodStart { get; set; }

    public int CallCount { get; set; }

    public int QuotaLimit { get; set; }

    public DateTimeOffset ResetsAt { get; set; }

    [Timestamp]
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}

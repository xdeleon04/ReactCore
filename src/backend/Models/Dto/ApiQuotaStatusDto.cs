namespace ReactCore.Backend.Models.Dto;

public sealed record ApiQuotaAlertDto(
    string Severity,
    string Message
);

public sealed record ApiQuotaStatusDto(
    string ApiName,
    int CallsToday,
    int CallsThisHour,
    int QuotaLimit,
    int QuotaUsed,
    int QuotaRemaining,
    int QuotaPercentage,
    DateTimeOffset ResetsAt,
    double SuccessRate,
    DateTimeOffset? LastCall,
    IReadOnlyList<ApiQuotaAlertDto> Alerts
);

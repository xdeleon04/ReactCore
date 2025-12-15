namespace ReactCore.Backend.Options;

public sealed class ExternalApiOptions
{
    public string ApiKey { get; init; } = string.Empty;
    public string BaseUrl { get; init; } = string.Empty;

    public int TimeoutSeconds { get; init; } = 10;
    public int CacheTtlMinutes { get; init; } = 30;
    public int RateLimitPerHour { get; init; } = 100;

    public int CircuitBreakerThreshold { get; init; } = 5;
    public int CircuitBreakerTimeoutSeconds { get; init; } = 60;
}

using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using ReactCore.Backend.Exceptions;
using ReactCore.Backend.Models.Dto;
using ReactCore.Backend.Options;

namespace ReactCore.Backend.Services;

/// <summary>
/// Calls OpenWeatherMap and maps responses to <see cref="WeatherDto"/>.
/// Implements retry/backoff and a simple in-memory circuit breaker.
/// </summary>
public sealed class WeatherApiService : IWeatherApiService
{
    private const string ApiName = "openweathermap";
    private const string CircuitBreakerKey = "external_api_circuit_openweathermap";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IMemoryCache _memoryCache;
    private readonly ExternalApiOptions _options;
    private readonly ILogger<WeatherApiService> _logger;

    public WeatherApiService(
        IHttpClientFactory httpClientFactory,
        IMemoryCache memoryCache,
        IOptions<ExternalApiOptions> options,
        ILogger<WeatherApiService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _memoryCache = memoryCache;
        _options = options.Value;
        _logger = logger;
    }

    /// <summary>
    /// Fetches weather for a location from OpenWeatherMap.
    /// Throws <see cref="ArgumentException"/> for invalid locations and <see cref="ApiUnavailableException"/> for external failures.
    /// </summary>
    public async Task<WeatherDto> FetchWeatherAsync(string location, CancellationToken cancellationToken = default)
    {
        var apiKey = ResolveApiKey(_options.ApiKey);
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new ApiUnavailableException("OpenWeatherMap API key is not configured.");
        }

        if (IsCircuitOpen(out var openUntilUtc) && openUntilUtc.HasValue)
        {
            throw new ApiUnavailableException($"OpenWeatherMap circuit breaker is open until {openUntilUtc:O}.");
        }

        var client = _httpClientFactory.CreateClient("OpenWeatherMap");

        const int maxRetries = 3;
        for (var attempt = 0; attempt <= maxRetries; attempt++)
        {
            if (attempt > 0)
            {
                var delaySeconds = (int)Math.Pow(2, attempt - 1);
                await Task.Delay(TimeSpan.FromSeconds(delaySeconds), cancellationToken);
            }

            try
            {
                var uri = BuildWeatherUri(location, apiKey);
                var stopwatch = Stopwatch.StartNew();

                using var response = await client.GetAsync(uri, cancellationToken);
                stopwatch.Stop();

                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    throw new ArgumentException("Location not found.", nameof(location));
                }

                if ((int)response.StatusCode >= 400)
                {
                    if (IsTransientStatus(response.StatusCode))
                    {
                        _logger.LogWarning(
                            "OpenWeatherMap transient status {StatusCode} on attempt {Attempt} (elapsed {ElapsedMs}ms)",
                            (int)response.StatusCode,
                            attempt + 1,
                            stopwatch.ElapsedMilliseconds);

                        continue;
                    }

                    var content = await SafeReadBodyAsync(response, cancellationToken);
                    throw new ApiUnavailableException($"OpenWeatherMap returned {(int)response.StatusCode}: {content}");
                }

                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                var dto = ParseWeather(json, fallbackLocation: location);

                RecordSuccess();
                return dto;
            }
            catch (ArgumentException)
            {
                throw;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                if (attempt == maxRetries)
                {
                    RecordFailure();
                    throw new ApiUnavailableException("OpenWeatherMap is unavailable.", ex);
                }

                _logger.LogWarning(ex, "OpenWeatherMap request attempt {Attempt} failed", attempt + 1);
            }
        }

        RecordFailure();
        throw new ApiUnavailableException("OpenWeatherMap is unavailable.");
    }

    private string BuildWeatherUri(string location, string apiKey)
    {
        var encodedLocation = Uri.EscapeDataString(location.Trim());

        // We intentionally rely on BaseAddress being configured on the named HttpClient.
        // The returned string here is a relative URI.
        return $"weather?q={encodedLocation}&appid={Uri.EscapeDataString(apiKey)}&units=metric";
    }

    private static string? ResolveApiKey(string rawApiKey)
    {
        var trimmed = rawApiKey?.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            return null;
        }

        // Support a common "${...}" placeholder style.
        // - "${OPENWEATHERMAP_API_KEY}" (template) => treat as NOT configured
        // - "${<actualKey>}" where actualKey looks like an OpenWeatherMap key => unwrap and use
        if (trimmed.StartsWith("${", StringComparison.Ordinal) && trimmed.EndsWith("}", StringComparison.Ordinal))
        {
            var inner = trimmed.Substring(2, trimmed.Length - 3).Trim();
            if (inner.Equals("OPENWEATHERMAP_API_KEY", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            // OpenWeatherMap keys are typically 32 hex chars.
            if (inner.Length == 32 && inner.All(static c => (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F')))
            {
                return inner;
            }

            return null;
        }

        // If it still contains a template marker, treat as unconfigured.
        if (trimmed.Contains("${", StringComparison.Ordinal))
        {
            return null;
        }

        return trimmed;
    }

    private static bool IsTransientStatus(HttpStatusCode statusCode)
    {
        return statusCode is HttpStatusCode.TooManyRequests
            or HttpStatusCode.RequestTimeout
            or HttpStatusCode.BadGateway
            or HttpStatusCode.ServiceUnavailable
            or HttpStatusCode.GatewayTimeout
            or HttpStatusCode.InternalServerError;
    }

    private static WeatherDto ParseWeather(string json, string fallbackLocation)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (!root.TryGetProperty("main", out var main))
        {
            throw new ApiUnavailableException("OpenWeatherMap response missing 'main'.");
        }

        if (!main.TryGetProperty("temp", out var tempElement) || tempElement.ValueKind != JsonValueKind.Number)
        {
            throw new ApiUnavailableException("OpenWeatherMap response missing 'main.temp'.");
        }

        var temperature = tempElement.GetDouble();

        var humidity = 0;
        if (main.TryGetProperty("humidity", out var humidityElement) && humidityElement.ValueKind == JsonValueKind.Number)
        {
            humidity = humidityElement.GetInt32();
        }

        var condition = "Unknown";
        if (root.TryGetProperty("weather", out var weatherArray)
            && weatherArray.ValueKind == JsonValueKind.Array
            && weatherArray.GetArrayLength() > 0)
        {
            var weather0 = weatherArray[0];
            if (weather0.TryGetProperty("main", out var conditionMain) && conditionMain.ValueKind == JsonValueKind.String)
            {
                condition = conditionMain.GetString() ?? condition;
            }
            else if (weather0.TryGetProperty("description", out var conditionDesc) && conditionDesc.ValueKind == JsonValueKind.String)
            {
                condition = conditionDesc.GetString() ?? condition;
            }
        }

        var windSpeed = 0d;
        if (root.TryGetProperty("wind", out var wind) && wind.ValueKind == JsonValueKind.Object)
        {
            if (wind.TryGetProperty("speed", out var windSpeedElement) && windSpeedElement.ValueKind == JsonValueKind.Number)
            {
                windSpeed = windSpeedElement.GetDouble();
            }
        }

        var resolvedLocation = fallbackLocation;
        if (root.TryGetProperty("name", out var nameElement) && nameElement.ValueKind == JsonValueKind.String)
        {
            resolvedLocation = nameElement.GetString() ?? resolvedLocation;
        }

        var now = DateTimeOffset.UtcNow;

        // Cache metadata is set by the orchestrator.
        return new WeatherDto(
            Temperature: temperature,
            Condition: condition,
            Location: resolvedLocation,
            Humidity: humidity,
            WindSpeed: windSpeed,
            FetchedAt: now,
            IsCached: false,
            CacheExpiresAt: now);
    }

    private bool IsCircuitOpen(out DateTimeOffset? openUntilUtc)
    {
        if (_memoryCache.TryGetValue<CircuitState>(CircuitBreakerKey, out var state)
            && state is not null
            && state.OpenUntilUtc.HasValue
            && state.OpenUntilUtc.Value > DateTimeOffset.UtcNow)
        {
            openUntilUtc = state.OpenUntilUtc;
            return true;
        }

        openUntilUtc = null;
        return false;
    }

    private void RecordFailure()
    {
        var state = _memoryCache.Get<CircuitState>(CircuitBreakerKey) ?? new CircuitState();
        state.ConsecutiveFailures++;

        if (state.ConsecutiveFailures >= Math.Max(1, _options.CircuitBreakerThreshold))
        {
            state.OpenUntilUtc = DateTimeOffset.UtcNow.AddSeconds(Math.Max(1, _options.CircuitBreakerTimeoutSeconds));
        }

        _memoryCache.Set(CircuitBreakerKey, state, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(Math.Max(10, _options.CircuitBreakerTimeoutSeconds * 2))
        });
    }

    private void RecordSuccess()
    {
        _memoryCache.Remove(CircuitBreakerKey);
    }

    private static async Task<string> SafeReadBodyAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            return await response.Content.ReadAsStringAsync(cancellationToken);
        }
        catch
        {
            return string.Empty;
        }
    }

    private sealed class CircuitState
    {
        public int ConsecutiveFailures { get; set; }
        public DateTimeOffset? OpenUntilUtc { get; set; }
    }
}

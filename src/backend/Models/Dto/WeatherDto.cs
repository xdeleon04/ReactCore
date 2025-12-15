namespace ReactCore.Backend.Models.Dto;

public sealed record WeatherDto(
    double Temperature,
    string Condition,
    string Location,
    int Humidity,
    double WindSpeed,
    DateTimeOffset FetchedAt,
    bool IsCached,
    DateTimeOffset CacheExpiresAt
);

using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PestiTrackBackend.Services.Weather;

public sealed class ExternalWeatherReading
{
    public DateTime RecordedAtUtc { get; init; }

    public decimal? TemperatureCelsius { get; init; }

    public decimal? HumidityPercentage { get; init; }

    public decimal? RainfallMillimeters { get; init; }

    public decimal? WindSpeedMetersPerSecond { get; init; }

    public decimal? WindDirectionDegrees { get; init; }

    public string? Conditions { get; init; }

    public string Source { get; init; } = "Open-Meteo";
}

public interface IExternalWeatherProvider
{
    Task<ExternalWeatherReading> FetchCurrentAsync(
        decimal latitude,
        decimal longitude,
        CancellationToken cancellationToken = default);
}

public sealed class OpenMeteoWeatherProvider : IExternalWeatherProvider
{
    public const string HttpClientName = "OpenMeteo";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly TimeProvider _timeProvider;

    public OpenMeteoWeatherProvider(
        IHttpClientFactory httpClientFactory,
        TimeProvider timeProvider)
    {
        _httpClientFactory = httpClientFactory;
        _timeProvider = timeProvider;
    }

    public async Task<ExternalWeatherReading> FetchCurrentAsync(
        decimal latitude,
        decimal longitude,
        CancellationToken cancellationToken = default)
    {
        var lat = latitude.ToString("0.######", CultureInfo.InvariantCulture);
        var lon = longitude.ToString("0.######", CultureInfo.InvariantCulture);

        var url =
            $"https://api.open-meteo.com/v1/forecast?latitude={lat}&longitude={lon}" +
            "&current=temperature_2m,relative_humidity_2m,precipitation,wind_speed_10m,wind_direction_10m,weather_code" +
            "&wind_speed_unit=ms&timezone=UTC";

        var client = _httpClientFactory.CreateClient(HttpClientName);
        using var response = await client.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var payload = await JsonSerializer.DeserializeAsync<OpenMeteoResponse>(
            stream,
            JsonOptions,
            cancellationToken);

        if (payload?.Current is null)
        {
            throw new InvalidOperationException(
                "Weather provider returned an unexpected response without current conditions.");
        }

        var current = payload.Current;
        var recordedAtUtc = ParseUtc(current.Time) ?? _timeProvider.GetUtcNow().UtcDateTime;

        return new ExternalWeatherReading
        {
            RecordedAtUtc = recordedAtUtc,
            TemperatureCelsius = current.Temperature2m,
            HumidityPercentage = current.RelativeHumidity2m,
            RainfallMillimeters = current.Precipitation,
            WindSpeedMetersPerSecond = current.WindSpeed10m,
            WindDirectionDegrees = current.WindDirection10m,
            Conditions = DescribeWmoCode(current.WeatherCode),
            Source = "Open-Meteo"
        };
    }

    private static DateTime? ParseUtc(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        if (DateTime.TryParse(
                value,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out var parsed))
        {
            return DateTime.SpecifyKind(parsed, DateTimeKind.Utc);
        }

        return null;
    }

    public static string DescribeWmoCode(int? code) => code switch
    {
        0 => "Clear sky",
        1 => "Mainly clear",
        2 => "Partly cloudy",
        3 => "Overcast",
        45 or 48 => "Fog",
        51 or 53 or 55 => "Drizzle",
        56 or 57 => "Freezing drizzle",
        61 or 63 or 65 => "Rain",
        66 or 67 => "Freezing rain",
        71 or 73 or 75 => "Snowfall",
        77 => "Snow grains",
        80 or 81 or 82 => "Rain showers",
        85 or 86 => "Snow showers",
        95 => "Thunderstorm",
        96 or 99 => "Thunderstorm with hail",
        _ => "Unknown"
    };

    private sealed class OpenMeteoResponse
    {
        [JsonPropertyName("current")]
        public OpenMeteoCurrent? Current { get; set; }
    }

    private sealed class OpenMeteoCurrent
    {
        [JsonPropertyName("time")]
        public string? Time { get; set; }

        [JsonPropertyName("temperature_2m")]
        public decimal? Temperature2m { get; set; }

        [JsonPropertyName("relative_humidity_2m")]
        public decimal? RelativeHumidity2m { get; set; }

        [JsonPropertyName("precipitation")]
        public decimal? Precipitation { get; set; }

        [JsonPropertyName("wind_speed_10m")]
        public decimal? WindSpeed10m { get; set; }

        [JsonPropertyName("wind_direction_10m")]
        public decimal? WindDirection10m { get; set; }

        [JsonPropertyName("weather_code")]
        public int? WeatherCode { get; set; }
    }
}

namespace PestiTrackBackend.Models.Weather;

public static class WeatherConditions
{
    public const string Cloudy = "Cloudy";
    public const string PartlyCloudy = "Partly Cloudy";
    public const string Overcast = "Overcast";
    public const string LightWind = "Light Wind";
    public const string StrongWind = "Strong Wind";
    public const string Humid = "Humid";

    public static readonly string[] All =
    [
        Cloudy,
        PartlyCloudy,
        Overcast,
        LightWind,
        StrongWind,
        Humid
    ];

    public static bool IsValid(string? value) =>
        value is not null &&
        All.Contains(value, StringComparer.Ordinal);
}

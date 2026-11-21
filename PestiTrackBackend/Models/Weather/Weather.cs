
namespace PestiTrackBackend.Models.Weather;

public sealed class WeatherRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid FarmId { get; set; }

    public Guid? FieldId { get; set; }

    public DateTime RecordedAtUtc { get; set; }

    public decimal? TemperatureCelsius { get; set; }

    public decimal? HumidityPercentage { get; set; }

    public decimal? RainfallMillimeters { get; set; }

    public decimal? WindSpeedMetersPerSecond { get; set; }

    public decimal? WindDirectionDegrees { get; set; }

    public string? Conditions { get; set; }

    public string Source { get; set; } = "Manual";

    public string? Notes { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public Farms.Farm Farm { get; set; } = null!;

    public Farms.Field? Field { get; set; }
}

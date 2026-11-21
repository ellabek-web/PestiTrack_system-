
namespace PestiTrackBackend.DTOs.Weather;

public sealed class WeatherRecordResponse
{
    public Guid Id { get; set; }
    public Guid FarmId { get; set; }
    public Guid? FieldId { get; set; }
    public DateTimeOffset RecordedAtUtc { get; set; }
    public decimal? TemperatureCelsius { get; set; }
    public decimal? HumidityPercentage { get; set; }
    public decimal? RainfallMillimeters { get; set; }
    public decimal? WindSpeedMetersPerSecond { get; set; }
    public decimal? WindDirectionDegrees { get; set; }
    public string? Conditions { get; set; }
    public string Source { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
}

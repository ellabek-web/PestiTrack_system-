
using System.ComponentModel.DataAnnotations;

namespace PestiTrackBackend.DTOs.Weather;

public sealed class UpdateWeatherRecordRequest
{
    [Required]
    public DateTimeOffset? RecordedAtUtc { get; set; }

    public Guid? FieldId { get; set; }

    [Range(typeof(decimal), "-100", "100")]
    public decimal? TemperatureCelsius { get; set; }

    [Range(typeof(decimal), "0", "100")]
    public decimal? HumidityPercentage { get; set; }

    [Range(typeof(decimal), "0", "99999999")]
    public decimal? RainfallMillimeters { get; set; }

    [Range(typeof(decimal), "0", "999999")]
    public decimal? WindSpeedMetersPerSecond { get; set; }

    [Range(typeof(decimal), "0", "360")]
    public decimal? WindDirectionDegrees { get; set; }

    [StringLength(100)]
    public string? Conditions { get; set; }

    [StringLength(100)]
    public string? Source { get; set; }

    [StringLength(1000)]
    public string? Notes { get; set; }
}

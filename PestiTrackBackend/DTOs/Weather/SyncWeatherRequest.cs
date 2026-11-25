using System.ComponentModel.DataAnnotations;

namespace PestiTrackBackend.DTOs.Weather;

public sealed class SyncWeatherRequest
{
    public Guid? FieldId { get; set; }

    [Range(typeof(decimal), "-90", "90")]
    public decimal? Latitude { get; set; }

    [Range(typeof(decimal), "-180", "180")]
    public decimal? Longitude { get; set; }

    [StringLength(1000)]
    public string? Notes { get; set; }
}

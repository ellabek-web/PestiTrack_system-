
namespace PestiTrackBackend.DTOs.Weather;

public sealed class WeatherRecordQueryRequest
{
    public Guid? FieldId { get; set; }

    public DateTimeOffset? FromUtc { get; set; }

    public DateTimeOffset? ToUtc { get; set; }
}


using PestiTrackBackend.Models.Authorization;
using PestiTrackBackend.Models.Crops;
using PestiTrackBackend.Models.Workers;
using PestiTrackBackend.Models.Weather;

namespace PestiTrackBackend.Models.Farms;

public sealed class Farm
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = string.Empty;

    public string Code { get; set; } = string.Empty;

    public string? Location { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public ICollection<FarmUser> UserAssignments { get; set; }
        = new List<FarmUser>();

    public ICollection<FarmRole> Roles { get; set; }
        = new List<FarmRole>();

    public ICollection<Field> Fields { get; set; }
        = new List<Field>();

    public ICollection<FieldCrop> FieldCrops { get; set; }
        = new List<FieldCrop>();

    public ICollection<Worker> Workers { get; set; }
        = new List<Worker>();

    public ICollection<WeatherRecord> WeatherRecords { get; set; }
        = new List<WeatherRecord>();
}
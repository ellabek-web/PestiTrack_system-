using PestiTrackBackend.Models.Crops;
using PestiTrackBackend.Models.Farms;
using PestiTrackBackend.Models.Identity;
using PestiTrackBackend.Models.Weather;
using PestiTrackBackend.Models.Workers;

namespace PestiTrackBackend.Models.Applications;

public sealed class PesticideApplication
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid FarmId { get; set; }

    public Guid FieldId { get; set; }

    public Guid FieldCropId { get; set; }

    public Guid WorkerId { get; set; }

    public DateTime ApplicationDateUtc { get; set; }

    public string TargetPest { get; set; } = string.Empty;

    public string ApplicationMethod { get; set; } = string.Empty;

    public string? Equipment { get; set; }

    public decimal AreaTreatedHectares { get; set; }

    public Guid? WeatherRecordId { get; set; }

    public string? Notes { get; set; }

    public PesticideApplicationStatus Status { get; set; }
        = PesticideApplicationStatus.Draft;

    public Guid CreatedByUserId { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public Farm Farm { get; set; } = null!;

    public Field Field { get; set; } = null!;

    public FieldCrop FieldCrop { get; set; } = null!;

    public Worker Worker { get; set; } = null!;

    public WeatherRecord? WeatherRecord { get; set; }

    public ApplicationUser CreatedByUser { get; set; } = null!;

    public ICollection<PesticideApplicationProduct> Products { get; set; }
        = new List<PesticideApplicationProduct>();
}
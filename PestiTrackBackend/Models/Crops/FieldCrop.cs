using PestiTrackBackend.Models.Farms;

namespace PestiTrackBackend.Models.Crops;

public sealed class FieldCrop
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid FarmId { get; set; }

    public Farm Farm { get; set; } = null!;

    public Guid FieldId { get; set; }
    public Field Field { get; set; } = null!;

    public Guid CropId { get; set; }
    public Crop Crop { get; set; } = null!;

    public DateOnly PlantingDate { get; set; }

    public DateOnly? ExpectedHarvestDate { get; set; }

    public DateOnly? ActualHarvestDate { get; set; }

    public decimal? AreaHectares { get; set; }

    public FieldCropStatus Status { get; set; }
        = FieldCropStatus.Planned;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
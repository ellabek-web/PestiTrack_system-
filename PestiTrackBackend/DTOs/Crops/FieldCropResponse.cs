using PestiTrackBackend.Models.Crops;

namespace PestiTrackBackend.DTOs.Crops;

public sealed class FieldCropResponse
{
    public Guid Id { get; set; }

    public Guid FarmId { get; set; }

    public Guid FieldId { get; set; }

    public Guid CropId { get; set; }

    public string CropName { get; set; } = string.Empty;

    public DateOnly PlantingDate { get; set; }

    public DateOnly? ExpectedHarvestDate { get; set; }

    public DateOnly? ActualHarvestDate { get; set; }

    public decimal? AreaHectares { get; set; }

    public FieldCropStatus Status { get; set; }

    public DateTime CreatedAtUtc { get; set; }
}
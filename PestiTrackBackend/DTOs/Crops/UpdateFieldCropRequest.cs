using System.ComponentModel.DataAnnotations;

namespace PestiTrackBackend.DTOs.Crops;

public sealed class UpdateFieldCropRequest
{
    [Required]
    public Guid CropId { get; set; }

    [Required]
    public DateOnly PlantingDate { get; set; }

    public DateOnly? ExpectedHarvestDate { get; set; }

    public DateOnly? ActualHarvestDate { get; set; }

    [Range(typeof(decimal), "0.001", "999999999999999.999")]
    public decimal? AreaHectares { get; set; }
}
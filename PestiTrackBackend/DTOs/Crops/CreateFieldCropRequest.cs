using System.ComponentModel.DataAnnotations;

namespace PestiTrackBackend.DTOs.Crops;

public sealed class CreateFieldCropRequest
{
    [Required]
    public Guid CropId { get; set; }

    [Required]
    public DateOnly PlantingDate { get; set; }

    public DateOnly? ExpectedHarvestDate { get; set; }

    [Range(typeof(decimal), "0.001", "999999999999999.999")]
    public decimal? AreaHectares { get; set; }
}
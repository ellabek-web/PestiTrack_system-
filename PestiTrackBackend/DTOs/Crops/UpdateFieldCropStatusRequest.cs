using System.ComponentModel.DataAnnotations;

namespace PestiTrackBackend.DTOs.Crops;

public sealed class UpdateFieldCropStatusRequest
{
    [Required]
    [RegularExpression(
        "^(Planned|Planted|Harvested|Cancelled)$",
        ErrorMessage = "Status must be Planned, Planted, Harvested, or Cancelled.")]
    public string Status { get; set; } = string.Empty;
}
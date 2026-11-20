using System.ComponentModel.DataAnnotations;

namespace PestiTrackBackend.DTOs.Crops;

public sealed class UpdateCropRequest
{
    [Required]
    [StringLength(150, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [StringLength(200)]
    public string? ScientificName { get; set; }

    [StringLength(500)]
    public string? Description { get; set; }
}
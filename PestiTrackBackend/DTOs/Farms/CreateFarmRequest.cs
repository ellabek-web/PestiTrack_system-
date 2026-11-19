using System.ComponentModel.DataAnnotations;

namespace PestiTrackBackend.DTOs.Farms;

public sealed class CreateFarmRequest
{
    [Required]
    [StringLength(150, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(50, MinimumLength = 2)]
    [RegularExpression(
        @"^[A-Za-z0-9_-]+$",
        ErrorMessage = "Farm code can contain only letters, numbers, hyphens and underscores.")]
    public string Code { get; set; } = string.Empty;

    [StringLength(300)]
    public string? Location { get; set; }
}
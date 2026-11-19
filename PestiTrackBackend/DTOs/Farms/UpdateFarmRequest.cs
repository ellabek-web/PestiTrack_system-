using System.ComponentModel.DataAnnotations;

namespace PestiTrackBackend.DTOs.Farms;

public sealed class UpdateFarmRequest
{
    [Required]
    [StringLength(150, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [StringLength(300)]
    public string? Location { get; set; }
}
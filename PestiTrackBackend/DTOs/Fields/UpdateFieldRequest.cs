
using System.ComponentModel.DataAnnotations;

namespace PestiTrackBackend.DTOs.Fields;

public sealed class UpdateFieldRequest
{
    [Required]
    [StringLength(150, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(50, MinimumLength = 2)]
    [RegularExpression(
        @"^[A-Za-z0-9_-]+$",
        ErrorMessage = "Field code may contain only letters, numbers, hyphens, and underscores.")]
    public string Code { get; set; } = string.Empty;

    [Range(typeof(decimal), "0.001", "999999999999999.999")]
    public decimal? AreaHectares { get; set; }

    [StringLength(500)]
    public string? Description { get; set; }
}

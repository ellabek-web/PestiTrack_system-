
using System.ComponentModel.DataAnnotations;

namespace PestiTrackBackend.DTOs.Products;

public sealed class UpdateProductRequest
{
    [Required]
    [StringLength(200, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [StringLength(300)]
    public string? ActiveIngredient { get; set; }

    [StringLength(200)]
    public string? Manufacturer { get; set; }

    [StringLength(100)]
    public string? RegistrationNumber { get; set; }

    [StringLength(100)]
    public string? Formulation { get; set; }

    [StringLength(150)]
    public string? Concentration { get; set; }

    [StringLength(100)]
    public string? HazardClass { get; set; }

    [StringLength(1000)]
    public string? Description { get; set; }
}

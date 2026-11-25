
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

    [StringLength(50)]
    public string? SignalWord { get; set; }

    public DateOnly? RegistrationExpiryDate { get; set; }

    [Range(0, 3650)]
    public int? PreHarvestIntervalDays { get; set; }

    [Range(0, 8760)]
    public int? ReEntryIntervalHours { get; set; }

    [Range(0.0001, 1000000)]
    public decimal? MaxApplicationRate { get; set; }

    [StringLength(50)]
    public string? MaxApplicationRateUnit { get; set; }

    [Range(0, 100)]
    public decimal? MaxWindSpeedMetersPerSecond { get; set; }

    [StringLength(2000)]
    public string? LabelNotes { get; set; }

    [StringLength(1000)]
    public string? Description { get; set; }
}

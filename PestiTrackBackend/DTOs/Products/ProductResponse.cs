
namespace PestiTrackBackend.DTOs.Products;

public sealed class ProductResponse
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? ActiveIngredient { get; set; }

    public string? Manufacturer { get; set; }

    public string? RegistrationNumber { get; set; }

    public string? Formulation { get; set; }

    public string? Concentration { get; set; }

    public string? HazardClass { get; set; }

    public string? SignalWord { get; set; }

    public DateOnly? RegistrationExpiryDate { get; set; }

    public int? PreHarvestIntervalDays { get; set; }

    public int? ReEntryIntervalHours { get; set; }

    public decimal? MaxApplicationRate { get; set; }

    public string? MaxApplicationRateUnit { get; set; }

    public decimal? MaxWindSpeedMetersPerSecond { get; set; }

    public string? LabelNotes { get; set; }

    public string? Description { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAtUtc { get; set; }
}


namespace PestiTrackBackend.Models.Products;

public sealed class Product
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = string.Empty;

    public string? ActiveIngredient { get; set; }

    public string? Manufacturer { get; set; }

    public string? RegistrationNumber { get; set; }

    public string? Formulation { get; set; }

    public string? Concentration { get; set; }

    public string? HazardClass { get; set; }

    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

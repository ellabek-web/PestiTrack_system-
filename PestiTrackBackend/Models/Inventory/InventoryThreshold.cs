using PestiTrackBackend.Models.Farms;
using PestiTrackBackend.Models.Products;

namespace PestiTrackBackend.Models.Inventory;

public sealed class InventoryThreshold
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid FarmId { get; set; }

    public Guid ProductId { get; set; }

    public decimal MinimumQuantity { get; set; }

    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    public Farm Farm { get; set; } = null!;

    public Product Product { get; set; } = null!;
}

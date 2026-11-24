using PestiTrackBackend.Models.Inventory;
using PestiTrackBackend.Models.Products;

namespace PestiTrackBackend.Models.Applications;

public sealed class PesticideApplicationProduct
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid PesticideApplicationId { get; set; }

    public Guid ProductId { get; set; }

    public string BatchNumber { get; set; } = string.Empty;

    public decimal QuantityUsed { get; set; }

    public decimal ApplicationRate { get; set; }

    public string RateUnit { get; set; } = string.Empty;

    public PesticideApplication PesticideApplication { get; set; } = null!;

    public Product Product { get; set; } = null!;

    public InventoryTransaction? InventoryTransaction { get; set; }
}
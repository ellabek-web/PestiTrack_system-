using PestiTrackBackend.Models.Farms;
using PestiTrackBackend.Models.Products;

namespace PestiTrackBackend.Models.Inventory;

public sealed class InventoryTransaction
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid FarmId { get; set; }

    public Guid ProductId { get; set; }

    public InventoryTransactionType TransactionType { get; set; }

    public decimal Quantity { get; set; }

    public string? BatchNumber { get; set; }

    public DateOnly? ExpiryDate { get; set; }

    public string? Reference { get; set; }

    public string? Reason { get; set; }

    public string? Notes { get; set; }

    public Guid CreatedByUserId { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public Farm Farm { get; set; } = null!;

    public Product Product { get; set; } = null!;
}
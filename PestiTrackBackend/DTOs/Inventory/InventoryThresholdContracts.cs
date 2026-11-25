using System.ComponentModel.DataAnnotations;

namespace PestiTrackBackend.DTOs.Inventory;

public sealed class UpsertInventoryThresholdRequest
{
    [Range(typeof(decimal), "0.001", "999999999999.999")]
    public decimal MinimumQuantity { get; set; }
}

public sealed class InventoryThresholdResponse
{
    public Guid Id { get; set; }

    public Guid FarmId { get; set; }

    public Guid ProductId { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public decimal MinimumQuantity { get; set; }

    public decimal CurrentQuantityOnHand { get; set; }

    public bool IsLowStock { get; set; }

    public DateTimeOffset UpdatedAtUtc { get; set; }
}

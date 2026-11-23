namespace PestiTrackBackend.DTOs.Inventory;

public sealed class InventoryBalanceResponse
{
    public Guid FarmId { get; set; }

    public Guid ProductId { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public string BatchNumber { get; set; } = string.Empty;

    public DateOnly? ExpiryDate { get; set; }

    public decimal QuantityOnHand { get; set; }
}
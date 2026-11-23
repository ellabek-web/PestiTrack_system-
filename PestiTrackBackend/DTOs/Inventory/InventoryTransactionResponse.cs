namespace PestiTrackBackend.DTOs.Inventory;

public sealed class InventoryTransactionResponse
{
    public Guid Id { get; set; }

    public Guid FarmId { get; set; }

    public Guid ProductId { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public string TransactionType { get; set; } = string.Empty;

    public decimal Quantity { get; set; }

    public string? BatchNumber { get; set; }

    public DateOnly? ExpiryDate { get; set; }

    public string? Reference { get; set; }

    public string? Reason { get; set; }

    public string? Notes { get; set; }

    public Guid CreatedByUserId { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
}
using System.ComponentModel.DataAnnotations;
using PestiTrackBackend.Models.Inventory;

namespace PestiTrackBackend.DTOs.Inventory;

public sealed class InventoryTransactionQueryRequest
{
    public Guid? ProductId { get; set; }

    [StringLength(100)]
    public string? BatchNumber { get; set; }

    public InventoryTransactionType? TransactionType { get; set; }

    public DateTimeOffset? FromUtc { get; set; }

    public DateTimeOffset? ToUtc { get; set; }

    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;

    [Range(1, 100)]
    public int PageSize { get; set; } = 20;
}
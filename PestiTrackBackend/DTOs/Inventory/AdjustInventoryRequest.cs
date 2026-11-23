using System.ComponentModel.DataAnnotations;

namespace PestiTrackBackend.DTOs.Inventory;

public sealed class AdjustInventoryRequest
{
    [Required]
    public Guid ProductId { get; set; }

    [Range(typeof(decimal), "0.001", "999999999999999.999")]
    public decimal Quantity { get; set; }

    [Required]
    [StringLength(100)]
    public string BatchNumber { get; set; } = string.Empty;

    public DateOnly? ExpiryDate { get; set; }

    [Required]
    public InventoryAdjustmentDirection? Direction { get; set; }

    [Required]
    [StringLength(500)]
    public string Reason { get; set; } = string.Empty;

    [StringLength(100)]
    public string? Reference { get; set; }

    [StringLength(1000)]
    public string? Notes { get; set; }
}
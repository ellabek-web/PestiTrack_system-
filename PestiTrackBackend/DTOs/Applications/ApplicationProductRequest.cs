using System.ComponentModel.DataAnnotations;

namespace PestiTrackBackend.DTOs.Applications;

public sealed class ApplicationProductRequest
{
    [Required]
    public Guid ProductId { get; set; }

    [Required]
    [StringLength(100)]
    public string BatchNumber { get; set; } = string.Empty;

    [Range(typeof(decimal), "0.001", "999999999999999.999")]
    public decimal QuantityUsed { get; set; }

    [Range(typeof(decimal), "0.001", "999999999999999.999")]
    public decimal ApplicationRate { get; set; }

    [Required]
    [StringLength(50)]
    public string RateUnit { get; set; } = string.Empty;
}
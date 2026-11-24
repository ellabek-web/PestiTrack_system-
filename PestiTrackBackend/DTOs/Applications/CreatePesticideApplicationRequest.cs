using System.ComponentModel.DataAnnotations;

namespace PestiTrackBackend.DTOs.Applications;

public sealed class CreatePesticideApplicationRequest
{
    [Required]
    public Guid FieldId { get; set; }

    [Required]
    public Guid FieldCropId { get; set; }

    [Required]
    public Guid WorkerId { get; set; }

    [Required]
    public DateTime ApplicationDateUtc { get; set; }

    [Required]
    [StringLength(200)]
    public string TargetPest { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string ApplicationMethod { get; set; } = string.Empty;

    [StringLength(200)]
    public string? Equipment { get; set; }

    [Range(typeof(decimal), "0.001", "999999999999999.999")]
    public decimal AreaTreatedHectares { get; set; }

    public Guid? WeatherRecordId { get; set; }

    [StringLength(1000)]
    public string? Notes { get; set; }

    [Required]
    [MinLength(1)]
    public List<ApplicationProductRequest> Products { get; set; } = new();
}
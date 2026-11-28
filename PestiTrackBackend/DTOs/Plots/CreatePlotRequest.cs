using System.ComponentModel.DataAnnotations;

namespace PestiTrackBackend.DTOs.Plots;

public sealed class CreatePlotRequest
{
    [Required]
    public Guid FieldId { get; set; }

    [Required]
    [StringLength(50, MinimumLength = 1)]
    public string PlotNumber { get; set; } = string.Empty;

    [Required]
    [StringLength(150, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [Range(typeof(decimal), "0.001", "999999999999999.999")]
    public decimal AreaValue { get; set; }

    [Required]
    [StringLength(20)]
    public string AreaUnit { get; set; } = string.Empty;

    [Required]
    [StringLength(300)]
    public string Location { get; set; } = string.Empty;

    [Required]
    public Guid CropId { get; set; }

    [StringLength(1000)]
    public string? Comment { get; set; }
}

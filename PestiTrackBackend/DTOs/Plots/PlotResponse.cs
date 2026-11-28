namespace PestiTrackBackend.DTOs.Plots;

public sealed class PlotResponse
{
    public Guid Id { get; set; }

    public Guid FarmId { get; set; }

    public Guid FieldId { get; set; }

    public string FieldName { get; set; } = string.Empty;

    public string PlotNumber { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public decimal AreaValue { get; set; }

    public string AreaUnit { get; set; } = string.Empty;

    public decimal AreaHectares { get; set; }

    public string Location { get; set; } = string.Empty;

    public Guid CropId { get; set; }

    public string CropName { get; set; } = string.Empty;

    public string? Comment { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAtUtc { get; set; }
}

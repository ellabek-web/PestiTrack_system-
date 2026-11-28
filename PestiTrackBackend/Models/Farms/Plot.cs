using PestiTrackBackend.Models.Crops;

namespace PestiTrackBackend.Models.Farms;

public sealed class Plot
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid FarmId { get; set; }
    public Farm Farm { get; set; } = null!;

    public Guid FieldId { get; set; }
    public Field Field { get; set; } = null!;

    public string PlotNumber { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public decimal AreaValue { get; set; }

    public string AreaUnit { get; set; } = PlotAreaUnits.Hectares;

    public string Location { get; set; } = string.Empty;

    public Guid CropId { get; set; }
    public Crop Crop { get; set; } = null!;

    public string? Comment { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public static class PlotAreaUnits
{
    public const string Hectares = "Hectares";
    public const string SquareMeters = "SquareMeters";
    public const string Acres = "Acres";

    public static readonly string[] All = [Hectares, SquareMeters, Acres];

    public static decimal ToHectares(decimal value, string unit) => unit switch
    {
        SquareMeters => Math.Round(value / 10000m, 4),
        Acres => Math.Round(value * 0.404686m, 4),
        _ => value
    };
}

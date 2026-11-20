namespace PestiTrackBackend.DTOs.Crops;

public sealed class CropResponse
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? ScientificName { get; set; }

    public string? Description { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAtUtc { get; set; }
}
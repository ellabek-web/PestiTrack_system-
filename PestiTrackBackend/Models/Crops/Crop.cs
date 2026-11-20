namespace PestiTrackBackend.Models.Crops;

public sealed class Crop
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = string.Empty;

    public string? ScientificName { get; set; }

    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public ICollection<FieldCrop> FieldAssignments { get; set; }
        = new List<FieldCrop>();
}
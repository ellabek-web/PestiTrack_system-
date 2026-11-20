using PestiTrackBackend.Models.Crops;

namespace PestiTrackBackend.Models.Farms;

public sealed class Field
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid FarmId { get; set; }
    public Farm Farm { get; set; } = null!;

    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;

    public decimal? AreaHectares { get; set; }

    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public ICollection<FieldCrop> CropAssignments { get; set; }
    = new List<FieldCrop>();
}

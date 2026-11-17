using PestiTrackBackend.Models.Farms;

namespace PestiTrackBackend.Models.Authorization;

public sealed class FarmRole
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid FarmId { get; set; }

    public Farm Farm { get; set; } = null!;

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool IsSystemRole { get; set; }

    public ICollection<FarmUserRole> UserAssignments { get; set; }
        = new List<FarmUserRole>();

    public ICollection<FarmRolePermission> Permissions { get; set; }
        = new List<FarmRolePermission>();
}
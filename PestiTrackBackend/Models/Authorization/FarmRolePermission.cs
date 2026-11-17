namespace PestiTrackBackend.Models.Authorization;

public sealed class FarmRolePermission
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid FarmRoleId { get; set; }

    public FarmRole FarmRole { get; set; } = null!;

    public Guid PermissionId { get; set; }

    public Permission Permission { get; set; } = null!;
}
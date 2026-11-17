namespace PestiTrackBackend.Models.Authorization;

public sealed class Permission
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public ICollection<FarmRolePermission> FarmRoles { get; set; }
        = new List<FarmRolePermission>();
}
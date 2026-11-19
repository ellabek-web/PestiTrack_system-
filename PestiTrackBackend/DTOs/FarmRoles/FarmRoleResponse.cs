namespace PestiTrackBackend.DTOs.FarmRoles;

public sealed class FarmRoleResponse
{
    public Guid Id { get; set; }

    public Guid FarmId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool IsSystemRole { get; set; }

    public List<string> PermissionCodes { get; set; } = [];
}
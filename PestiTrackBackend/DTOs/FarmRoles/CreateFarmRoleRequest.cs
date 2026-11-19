using System.ComponentModel.DataAnnotations;

namespace PestiTrackBackend.DTOs.FarmRoles;

public sealed class CreateFarmRoleRequest
{
    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [StringLength(300)]
    public string? Description { get; set; }

    [Required]
    [MinLength(1)]
    public List<string> PermissionCodes { get; set; } = [];
}
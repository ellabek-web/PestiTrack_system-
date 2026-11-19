using System.ComponentModel.DataAnnotations;

namespace PestiTrackBackend.DTOs.FarmUsers;

public sealed class UpdateFarmUserRolesRequest
{
    [Required]
    [MinLength(1)]
    public List<Guid> FarmRoleIds { get; set; } = [];
}
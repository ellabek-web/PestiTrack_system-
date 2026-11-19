using System.ComponentModel.DataAnnotations;

namespace PestiTrackBackend.DTOs.FarmUsers;

public sealed class AssignFarmUserRequest
{
    [Required]
    public Guid UserId { get; set; }

    [Required]
    [MinLength(1)]
    public List<Guid> FarmRoleIds { get; set; } = [];
}
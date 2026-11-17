using PestiTrackBackend.Models.Identity;

namespace PestiTrackBackend.Models.Farms;

public sealed class FarmUser
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid FarmId { get; set; }

    public Farm Farm { get; set; } = null!;

    public Guid UserId { get; set; }

    public ApplicationUser User { get; set; } = null!;

    public bool IsActive { get; set; } = true;

    public DateTime AssignedAtUtc { get; set; } = DateTime.UtcNow;

    public ICollection<FarmUserRole> Roles { get; set; }
        = new List<FarmUserRole>();
}
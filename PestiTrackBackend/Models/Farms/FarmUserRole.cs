using PestiTrackBackend.Models.Authorization;

namespace PestiTrackBackend.Models.Farms;

public sealed class FarmUserRole
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid FarmId { get; set; }

    public Guid FarmUserId { get; set; }

    public FarmUser FarmUser { get; set; } = null!;

    public Guid FarmRoleId { get; set; }

    public FarmRole FarmRole { get; set; } = null!;

    public DateTime AssignedAtUtc { get; set; } = DateTime.UtcNow;
}
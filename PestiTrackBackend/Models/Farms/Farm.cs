
using PestiTrackBackend.Models.Authorization;
using PestiTrackBackend.Models.Farms;
namespace PestiTrackBackend.Models.Farms;

public sealed class Farm
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = string.Empty;

    public string Code { get; set; } = string.Empty;

    public string? Location { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public ICollection<FarmUser> UserAssignments { get; set; }
        = new List<FarmUser>();

    public ICollection<FarmRole> Roles { get; set; }
        = new List<FarmRole>();
}
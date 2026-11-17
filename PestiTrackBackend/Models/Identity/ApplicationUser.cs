using Microsoft.AspNetCore.Identity;
using PestiTrackBackend.Models.Farms;

namespace PestiTrackBackend.Models.Identity;

public sealed class ApplicationUser : IdentityUser<Guid>
{
    public string DisplayName { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public ICollection<FarmUser> FarmAssignments { get; set; }
        = new List<FarmUser>();
}
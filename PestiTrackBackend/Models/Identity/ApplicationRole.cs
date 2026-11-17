using Microsoft.AspNetCore.Identity;

namespace PestiTrackBackend.Models.Identity;

public sealed class ApplicationRole : IdentityRole<Guid>
{
    public string? Description { get; set; }

    public bool IsSystemRole { get; set; }
}
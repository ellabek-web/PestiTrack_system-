using PestiTrackBackend.Models.Identity;

namespace PestiTrackBackend.Security;

public static class SystemRoleSeedData
{
    public static readonly ApplicationRole[] All =
    [
        new()
        {
            Id = Guid.Parse("20000000-0000-0000-0000-000000000001"),
            Name = SystemRoleCodes.Administrator,
            NormalizedName = SystemRoleCodes.Administrator.ToUpperInvariant(),
            Description = "Full system-wide administrative access.",
            IsSystemRole = true,
            ConcurrencyStamp = "20000000-0000-0000-0000-000000000001"
        },
        new()
        {
            Id = Guid.Parse("20000000-0000-0000-0000-000000000002"),
            Name = SystemRoleCodes.Auditor,
            NormalizedName = SystemRoleCodes.Auditor.ToUpperInvariant(),
            Description = "Read-only system-wide audit access.",
            IsSystemRole = true,
            ConcurrencyStamp = "20000000-0000-0000-0000-000000000002"
        }
    ];
}
namespace PestiTrackBackend.Data;

/// <summary>
/// One row of the ASP.NET Core Data Protection key ring, stored in the
/// <c>DataProtectionKeys</c> table.
///
/// The login cookie and the CSRF tokens are encrypted with these keys.
/// Keeping them in the database (rather than the container filesystem)
/// is what makes logins survive Render restarts and redeploys: a fresh
/// container reads the same keys back instead of generating new ones
/// and invalidating every existing session.
///
/// The shape deliberately matches the table the standard
/// <c>Microsoft.AspNetCore.DataProtection.EntityFrameworkCore</c>
/// provider creates (<c>Id</c> / <c>FriendlyName</c> / <c>Xml</c>), so
/// switching to <c>PersistKeysToDbContext</c> later needs no migration.
/// </summary>
public sealed class PersistedDataProtectionKey
{
    public int Id { get; set; }

    public string? FriendlyName { get; set; }

    public string Xml { get; set; } = string.Empty;
}

using System.Xml.Linq;
using Microsoft.AspNetCore.DataProtection.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PestiTrackBackend.Data;

namespace PestiTrackBackend.Security.DataProtection;

/// <summary>
/// Stores the ASP.NET Core Data Protection key ring in the application
/// database (<c>DataProtectionKeys</c> table) instead of the container
/// filesystem.
///
/// Why a small custom <see cref="IXmlRepository"/> instead of the
/// standard <c>Microsoft.AspNetCore.DataProtection.EntityFrameworkCore</c>
/// package: it needs no extra NuGet package, so it works with the
/// packages already restored here and on Render, and it works for both
/// providers (SQL Server locally, PostgreSQL on Neon) through the
/// existing <see cref="AppDbContext"/> — the keys table is created by
/// the normal migrations, and the entrypoint already runs
/// <c>--migrate</c> on container start.
///
/// The repository is a singleton but opens a fresh scope (and therefore
/// a fresh <see cref="AppDbContext"/>) per call — a scoped context must
/// never be captured by a singleton. The key manager calls
/// <see cref="GetAllElements"/> at startup and <see cref="StoreElement"/>
/// when it creates a new key (every 90 days by default, or on first
/// boot when the table is empty).
/// </summary>
public sealed class DatabaseDataProtectionKeyRepository : IXmlRepository
{
    private readonly IServiceScopeFactory _scopeFactory;

    public DatabaseDataProtectionKeyRepository(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public IReadOnlyCollection<XElement> GetAllElements()
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return db.DataProtectionKeys
            .AsNoTracking()
            .OrderBy(key => key.Id)
            .Select(key => XElement.Parse(key.Xml))
            .ToList();
    }

    public void StoreElement(XElement element, string friendlyName)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.DataProtectionKeys.Add(new PersistedDataProtectionKey
        {
            FriendlyName = friendlyName,
            Xml = element.ToString(SaveOptions.DisableFormatting),
        });
        db.SaveChanges();
    }
}

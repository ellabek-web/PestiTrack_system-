using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.DataProtection.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using PestiTrackBackend.Data;
using PestiTrackBackend.Security.DataProtection;

namespace PestiTrackBackend.Tests;

/// <summary>
/// Guards the session-survival fix: Data Protection keys must live in
/// the database, so a login issued before a restart still decrypts
/// after it. The two providers below share one in-memory database and
/// stand in for the Render container before and after a restart.
/// </summary>
public sealed class DataProtectionKeyPersistenceTests
{
    private static ServiceProvider BuildProvider(InMemoryDatabaseRoot root)
    {
        var services = new ServiceCollection();
        services.AddDbContext<AppDbContext>(options =>
            options.UseInMemoryDatabase("dp-keys-test", root));
        services.AddSingleton<
            IXmlRepository,
            DatabaseDataProtectionKeyRepository>();
        services.AddDataProtection()
            .SetApplicationName("PestiTrack");
        services.AddOptions<KeyManagementOptions>()
            .Configure<IXmlRepository>(
                (options, repository) => options.XmlRepository = repository);
        return services.BuildServiceProvider();
    }

    [Fact]
    public void ProtectedValue_UnprotectsAfterRestart_WithKeysInDatabase()
    {
        var root = new InMemoryDatabaseRoot();

        string protectedPayload;
        using (var before = BuildProvider(root))
        {
            var protector = before.GetRequiredService<IDataProtectionProvider>()
                .CreateProtector("test-purpose", "v1");
            protectedPayload = protector.Protect("session-payload");

            using var scope = before.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            Assert.True(db.DataProtectionKeys.Any());
        }

        // Simulated restart: a brand-new provider, same database.
        using (var after = BuildProvider(root))
        {
            var protector = after.GetRequiredService<IDataProtectionProvider>()
                .CreateProtector("test-purpose", "v1");
            Assert.Equal("session-payload", protector.Unprotect(protectedPayload));
        }
    }
}

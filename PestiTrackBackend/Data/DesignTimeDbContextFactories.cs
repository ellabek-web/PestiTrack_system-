using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace PestiTrackBackend.Data;

/// <summary>
/// Design-time factory so <c>dotnet ef</c> can build the PostgreSQL
/// context without running the web host:
/// <code>
/// # PostgreSQL (Neon / Supabase)
/// dotnet ef migrations add &lt;Name&gt; --context PostgresAppDbContext \
///   --project PestiTrackBackend --output-dir Data/Migrations/Postgres
///
/// # SQL Server (local, default) — AppDbContext itself is the SQL Server
/// # context, and its migrations live in Data/Migrations:
/// dotnet ef migrations add &lt;Name&gt; --context AppDbContext \
///   --project PestiTrackBackend --output-dir Data/Migrations
/// </code>
/// The provider is forced by the context type, not by configuration — so
/// generating Postgres migrations never depends on which connection string
/// happens to be in <c>appsettings.Development.json</c>. A dummy local
/// connection string is used; no database connection is opened while
/// scaffolding a migration.
/// </summary>
public sealed class PostgresDesignTimeDbContextFactory
    : IDesignTimeDbContextFactory<PostgresAppDbContext>
{
    public PostgresAppDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<PostgresAppDbContext>()
            .UseNpgsql(
                "Host=localhost;Port=5432;Database=PestiTrackDb;Username=postgres;Password=postgres")
            .Options;
        return new PostgresAppDbContext(options);
    }
}

/// <summary>
/// Design-time factory for the base <see cref="AppDbContext"/> (used by
/// tools that target the base type, and by <c>dotnet ef dbcontext info</c>).
/// Resolves the provider from configuration exactly like
/// <c>Program.cs</c> does at runtime.
/// </summary>
public sealed class AppDbContextDesignTimeFactory
    : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var (provider, connectionString) = DatabaseOptions.Resolve(configuration);
        var builder = new DbContextOptionsBuilder<AppDbContext>();
        if (provider == DatabaseProvider.PostgreSql)
        {
            builder.UseNpgsql(connectionString);
        }
        else
        {
            builder.UseSqlServer(connectionString);
        }

        return new AppDbContext(builder.Options);
    }
}

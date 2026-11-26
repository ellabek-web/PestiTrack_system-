using Microsoft.EntityFrameworkCore;

namespace PestiTrackBackend.Data;

/// <summary>
/// PostgreSQL flavour of <see cref="AppDbContext"/>.
///
/// Why a derived context instead of one context for both providers?
///
/// EF Core keeps one model snapshot per context type, and the snapshot is
/// provider-specific (SQL Server uses <c>nvarchar</c> / identity columns,
/// PostgreSQL uses <c>text</c> / <c>uuid</c> / identity-by-default). A single
/// context cannot carry both snapshots, and a single migrations folder
/// cannot hold both providers' migrations — <c>dotnet ef database
/// update</c> would try to apply both sets.
///
/// So the providers split like this:
/// <list type="bullet">
/// <item><description><see cref="AppDbContext"/> itself is the SQL Server
/// context — <c>Data/Migrations</c> holds the original 15 migrations and
/// snapshot, untouched, so an existing SQL Server database is never
/// re-migrated.</description></item>
/// <item><description><see cref="PostgresAppDbContext"/> — the PostgreSQL
/// context, with its own migrations folder
/// <c>Data/Migrations/Postgres</c> and its own snapshot.</description></item>
/// </list>
/// At runtime <c>Program.cs</c> registers the matching context and
/// exposes it as <see cref="AppDbContext"/> — every controller, service and
/// test keeps injecting <see cref="AppDbContext"/> and never sees the
/// difference. Tests keep using <see cref="AppDbContext"/> directly with the
/// in-memory provider, also unchanged.
/// </summary>
public sealed class PostgresAppDbContext : AppDbContext
{
    public PostgresAppDbContext(DbContextOptions<PostgresAppDbContext> options)
        : base(options)
    {
    }
}

using Microsoft.Extensions.Configuration;

namespace PestiTrackBackend.Data;

/// <summary>
/// Which database engine the API talks to.
///
/// <list type="bullet">
/// <item><description><see cref="SqlServer"/> — local development / on-prem.
/// The original provider. Connection string is a SQL Server string
/// (<c>Server=...;Database=...;User Id=...;Password=...</c>).</description></item>
/// <item><description><see cref="PostgreSql"/> — free cloud deployment.
/// PostgreSQL on Neon or Supabase (free tier, no credit card).
/// Connection string is either an Npgsql string
/// (<c>Host=...;Username=...;Password=...;Database=...</c>) or a URL
/// (<c>postgres://user:password@host:port/database?sslmode=require</c>,
/// exactly what Neon / Supabase hand you).</description></item>
/// </list>
/// </summary>
public enum DatabaseProvider
{
    SqlServer,
    PostgreSql
}

/// <summary>
/// Resolves the database provider + connection string from configuration.
///
/// Priority (first match wins):
/// <list type="number">
/// <item><description><c>DATABASE_URL</c> env var — if present and it looks like
/// a Postgres URL (<c>postgres://</c> / <c>postgresql://</c>), the provider is
/// PostgreSQL. This is what Render, Neon and Supabase use, so a Render
/// service wired to a Neon database needs no other setting.</description></item>
/// <item><description><c>Database:Provider</c> setting
/// (<c>DATABASE_PROVIDER</c> env var, or <c>Database__Provider</c> on Render)
/// — <c>PostgreSql</c> / <c>Postgres</c> / <c>Npgsql</c>, <c>SqlServer</c> /
/// <c>Sql</c> / <c>Mssql</c>, or <c>Auto</c> (default).</description></item>
/// <item><description><c>Database:ConnectionString</c> /
/// <c>ConnectionStrings:DefaultConnection</c> — auto-detected: a value
/// starting with <c>postgres://</c> or containing <c>Host=</c> is PostgreSQL,
/// anything else is SQL Server.</description></item>
/// </list>
/// If no connection string is configured at all, resolution fails fast with a
/// clear message instead of a cryptic EF Core error at first request.
/// </summary>
public static class DatabaseOptions
{
    public const string DefaultSqlServerConnectionString =
        "Server=(localdb)\\mssqllocaldb;Database=PestiTrackDb;Trusted_Connection=True;TrustServerCertificate=True";

    public static (DatabaseProvider Provider, string ConnectionString) Resolve(
        IConfiguration configuration)
    {
        // 1. DATABASE_URL wins — Render / Neon / Supabase convention.
        var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
        if (!string.IsNullOrWhiteSpace(databaseUrl))
        {
            if (IsPostgresUrl(databaseUrl))
            {
                return (DatabaseProvider.PostgreSql,
                    ConvertDatabaseUrlToNpgsqlConnectionString(databaseUrl));
            }

            // A DATABASE_URL that is not a Postgres URL is a configuration
            // error, not something to silently reinterpret as SQL Server.
            throw new InvalidOperationException(
                "DATABASE_URL is set but is not a postgres:// URL. " +
                "Unset it for SQL Server, or set it to the Neon/Supabase Postgres URL.");
        }

        // 2. Explicit provider choice.
        var providerSetting = configuration["Database:Provider"]
            ?? Environment.GetEnvironmentVariable("DATABASE_PROVIDER");
        var provider = ParseProvider(providerSetting);

        // 3. Connection string: Database:ConnectionString, then the
        // long-standing ConnectionStrings:DefaultConnection.
        var connectionString = configuration["Database:ConnectionString"]
            ?? configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "No database connection string is configured. Set " +
                "ConnectionStrings:DefaultConnection (SQL Server), " +
                "Database:ConnectionString, or DATABASE_URL " +
                "(Neon/Supabase postgres:// URL). See .env.example.");
        }

        connectionString = connectionString.Trim();

        if (provider is null)
        {
            // Auto: detect from the connection string itself.
            provider = IsPostgresConnectionString(connectionString)
                ? DatabaseProvider.PostgreSql
                : DatabaseProvider.SqlServer;
        }

        if (provider == DatabaseProvider.PostgreSql
            && IsPostgresUrl(connectionString))
        {
            connectionString =
                ConvertDatabaseUrlToNpgsqlConnectionString(connectionString);
        }

        return (provider.Value, connectionString);
    }

    public static DatabaseProvider? ParseProvider(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim().ToLowerInvariant() switch
        {
            "auto" or "" => null,
            "postgres" or "postgresql" or "npgsql" or "pgsql" or "pg"
                => DatabaseProvider.PostgreSql,
            "sqlserver" or "sql" or "mssql" or "sql-server"
                => DatabaseProvider.SqlServer,
            _ => throw new InvalidOperationException(
                $"Unknown Database:Provider '{value}'. " +
                "Use 'SqlServer', 'PostgreSql', or 'Auto'.")
        };
    }

    public static bool IsPostgresUrl(string value) =>
        value.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase)
        || value.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase);

    public static bool IsPostgresConnectionString(string value) =>
        IsPostgresUrl(value)
        || value.Contains("Host=", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Converts a <c>postgres://user:pass@host:port/db?sslmode=require</c> URL
    /// (Neon / Supabase format) into an Npgsql connection string.
    /// Neon and Supabase both require SSL; the pooled Neon endpoint also
    /// needs the <c>endpoint</c> parameter stripped out of the query string.
    /// </summary>
    public static string ConvertDatabaseUrlToNpgsqlConnectionString(string url)
    {
        var uri = new Uri(url);
        var userInfo = Uri.UnescapeDataString(uri.UserInfo).Split(':', 2);
        var username = userInfo[0];
        var password = userInfo.Length > 1 ? userInfo[1] : string.Empty;
        var database = Uri.UnescapeDataString(
            uri.AbsolutePath.TrimStart('/'));

        // Parse the query string (sslmode, etc.) without System.Web.
        var query = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (!string.IsNullOrEmpty(uri.Query))
        {
            foreach (var pair in uri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
            {
                var parts = pair.Split('=', 2);
                if (parts.Length == 2)
                {
                    query[Uri.UnescapeDataString(parts[0])] =
                        Uri.UnescapeDataString(parts[1]);
                }
            }
        }

        // Neon pooled URLs carry ?options=endpoint%3D... — Npgsql does not
        // understand it as a connection-string key, so drop it (the host
        // already routes to the endpoint).
        query.Remove("options");

        var sslMode = query.TryGetValue("sslmode", out var mode)
            ? mode.ToLowerInvariant() switch
            {
                "disable" => "Disable",
                "prefer" => "Prefer",
                "require" or "verify-ca" or "verify-full" => "Require",
                _ => "Require"
            }
            : "Require";

        var builder = new System.Text.StringBuilder();
        builder.Append($"Host={uri.Host};");
        if (uri.Port > 0)
        {
            builder.Append($"Port={uri.Port};");
        }

        builder.Append($"Database={database};");
        builder.Append($"Username={username};");
        builder.Append($"Password={password};");
        builder.Append($"SSL Mode={sslMode};");
        // Neon / Supabase use publicly trusted certs, but Render's
        // container CA bundle has historically missed intermediates;
        // trusting the server cert avoids a deploy-time TLS failure
        // without weakening anything else (credentials still required).
        builder.Append("Trust Server Certificate=true;");
        // Small pool: Render free + Neon free tier both cap connections
        // (Neon free: ~100 total, Render free sleeps). Keep the pool
        // small so one instance cannot exhaust the database.
        builder.Append("Pooling=true;Minimum Pool Size=0;Maximum Pool Size=10;");
        builder.Append("Timeout=15;Command Timeout=30;");

        return builder.ToString();
    }
}

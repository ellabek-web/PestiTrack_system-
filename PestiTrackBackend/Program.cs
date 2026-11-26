
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using PestiTrackBackend.Data;
using PestiTrackBackend.Health;
using PestiTrackBackend.Models.Identity;
using PestiTrackBackend.Security.Bootstrap;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi;
using PestiTrackBackend.Swagger;
using PestiTrackBackend.Services.Authorization;
using PestiTrackBackend.Security.Authorization;
using PestiTrackBackend.Security.Middleware;
using PestiTrackBackend.Security.RateLimiting;
using Microsoft.AspNetCore.Authorization;
using PestiTrackBackend.Services.Auditing;
using PestiTrackBackend.Services.Notifications;
using PestiTrackBackend.Services.Weather;



if (args.Contains("--healthcheck", StringComparer.OrdinalIgnoreCase))
{
    var probeUrl = Environment.GetEnvironmentVariable("HEALTHCHECK_URL")
        ?? "http://127.0.0.1:8080/health/live";
    try
    {
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(4) };
        using var response = await client.GetAsync(probeUrl);
        Environment.ExitCode = response.IsSuccessStatusCode ? 0 : 1;
    }
    catch
    {
        Environment.ExitCode = 1;
    }

    return;
}

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<AuditSaveChangesInterceptor>();

// Register Entity Framework Core with the configured provider:
// SQL Server locally, PostgreSQL (Neon / Supabase) in the cloud.
// See Data/DatabaseProvider.cs for how the provider is resolved:
// DATABASE_URL (postgres://...) wins, then Database:Provider
// (DATABASE_PROVIDER), then auto-detection from the connection string.
// SQL Server uses AppDbContext itself (its migrations live in
// Data/Migrations, untouched); PostgreSQL uses the derived
// PostgresAppDbContext with its own migrations in
// Data/Migrations/Postgres, because EF Core keeps one
// provider-specific model snapshot per context type.
var (databaseProvider, databaseConnectionString) =
    DatabaseOptions.Resolve(builder.Configuration);

if (databaseProvider == DatabaseProvider.PostgreSql)
{
    builder.Services.AddDbContext<PostgresAppDbContext>((services, options) =>
    {
        options.UseNpgsql(databaseConnectionString);
        options.AddInterceptors(
            services.GetRequiredService<AuditSaveChangesInterceptor>());
    });
    // Controllers / services inject AppDbContext; give them the
    // Postgres instance.
    builder.Services.AddScoped<AppDbContext>(services =>
        services.GetRequiredService<PostgresAppDbContext>());
}
else
{
    builder.Services.AddDbContext<AppDbContext>((services, options) =>
    {
        options.UseSqlServer(databaseConnectionString);
        options.AddInterceptors(
            services.GetRequiredService<AuditSaveChangesInterceptor>());
    });
}


// Persist ASP.NET Core Data Protection keys in the database, not the
// container filesystem: the login cookie and CSRF tokens are encrypted
// with these keys, and without this every Render restart/redeploy
// generated new keys and invalidated every existing login (401 on
// /api/auth/me).
//
// Wiring note: AddDataProtection does NOT pick up an IXmlRepository
// from DI on its own — KeyManagementOptions.XmlRepository must be set
// explicitly (verified by a round-trip harness: without this line the
// repository is never called and keys stay in memory). The keys table
// is created by the normal migrations (entrypoint runs --migrate on
// start), so no extra startup step is needed. The one redeploy that
// ships this logs everyone out once — the old keys are lost — after
// that, logins survive restarts.
builder.Services.AddSingleton<
    Microsoft.AspNetCore.DataProtection.Repositories.IXmlRepository,
    PestiTrackBackend.Security.DataProtection.DatabaseDataProtectionKeyRepository>();
builder.Services.AddDataProtection()
    .SetApplicationName("PestiTrack");
builder.Services.AddOptions<
    Microsoft.AspNetCore.DataProtection.KeyManagement.KeyManagementOptions>()
    .Configure<
        Microsoft.AspNetCore.DataProtection.Repositories.IXmlRepository>(
        (options, repository) => options.XmlRepository = repository);

// Configure ASP.NET Core Identity.
builder.Services
    .AddIdentityCore<ApplicationUser>(options =>
    {
        // Password requirements.
        options.Password.RequiredLength = 8;
        options.Password.RequiredUniqueChars = 1;
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = false;
        options.Password.RequireUppercase = false;
        options.Password.RequireNonAlphanumeric = false;

        // User requirements.
        options.User.RequireUniqueEmail = true;

        // Account lockout.
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan =
            TimeSpan.FromMinutes(15);
        options.Lockout.AllowedForNewUsers = true;
    })
    .AddRoles<ApplicationRole>()
    .AddEntityFrameworkStores<AppDbContext>()
    .AddClaimsPrincipalFactory<
        PestiTrackUserClaimsPrincipalFactory>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

builder.Services.Configure<SecurityStampValidatorOptions>(options =>
{
    // Quickly invalidate sessions after password, status, or role changes.
    options.ValidationInterval = TimeSpan.FromMinutes(1);
});

// Configure cookie-based authentication.
builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme =
            IdentityConstants.ApplicationScheme;

        options.DefaultChallengeScheme =
            IdentityConstants.ApplicationScheme;

        options.DefaultSignInScheme =
            IdentityConstants.ApplicationScheme;
    })
    .AddCookie(IdentityConstants.ApplicationScheme, options =>
    {
        options.Cookie.Name = "__Host-PestiTrack.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.Path = "/";

        // Host-only cookie: do not configure Cookie.Domain.
        options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
        options.SlidingExpiration = true;

        options.Events.OnValidatePrincipal =
            SecurityStampValidator.ValidatePrincipalAsync;

        // Return HTTP status codes instead of redirecting API clients.
        options.Events.OnRedirectToLogin = context =>
        {
            context.Response.StatusCode =
                StatusCodes.Status401Unauthorized;

            return Task.CompletedTask;
        };

        options.Events.OnRedirectToAccessDenied = context =>
        {
            context.Response.StatusCode =
                StatusCodes.Status403Forbidden;

            return Task.CompletedTask;
        };
    });

// Register authorization services.
builder.Services.AddAuthorization();

// Register farm-level permission authorization service.
builder.Services.AddScoped<
    IFarmAuthorizationService,
    FarmAuthorizationService>();

builder.Services.AddHttpClient(OpenMeteoWeatherProvider.HttpClientName, client =>
{
    client.Timeout = TimeSpan.FromSeconds(15);
    client.DefaultRequestHeaders.UserAgent.ParseAdd("PestiTrack/1.0");
});
builder.Services.AddScoped<IExternalWeatherProvider, OpenMeteoWeatherProvider>();
builder.Services.AddScoped<IFarmNotificationService, FarmNotificationService>();

// Register the dynamic farm permission policy provider.
builder.Services.AddSingleton<
    IAuthorizationPolicyProvider,
    FarmPermissionPolicyProvider>();

// Register the farm permission authorization handler.
builder.Services.AddScoped<
    IAuthorizationHandler,
    FarmPermissionAuthorizationHandler>();


// Configure CSRF protection for browser requests.
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";

    options.Cookie.Name = "__Host-PestiTrack.Csrf";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.Path = "/";

    // Do not configure Cookie.Domain.
});

// Register administrator provisioning service.
builder.Services.AddScoped<AdminBootstrapper>();

// Configure global exception handling and RFC 7807 ProblemDetails.
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

// Configure reverse-proxy forwarded headers.
var trustForwardedHeaders = builder.Configuration.GetValue("ReverseProxy:TrustForwardedHeaders", false);
var terminateTlsExternally = builder.Configuration.GetValue("ReverseProxy:TerminateTlsExternally", false);
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
        ForwardedHeaders.XForwardedFor |
        ForwardedHeaders.XForwardedProto;

    if (trustForwardedHeaders)
    {
        options.KnownIPNetworks.Clear();
        options.KnownProxies.Clear();
    }
});

// Configure optional trusted CORS origins when frontend is hosted on a separate origin.
var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>()
    ?.Where(origin => !string.IsNullOrWhiteSpace(origin))
    .ToArray() ?? [];

if (allowedOrigins.Length > 0)
{
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("TrustedOrigins", policy =>
        {
            policy.WithOrigins(allowedOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        });
    });
}

// Configure rate limiting (strict auth policy + global per-IP protection).
var globalLimit = Math.Max(
    20,
    builder.Configuration.GetValue("RateLimiting:GlobalPermitLimitPerMinute", 200));
var authLimit = Math.Max(
    3,
    builder.Configuration.GetValue("RateLimiting:AuthPermitLimitPerMinute", 10));

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, cancellationToken) =>
    {
        var retryAfterSeconds = 60;
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            retryAfterSeconds = Math.Max(1, (int)Math.Ceiling(retryAfter.TotalSeconds));
        }

        context.HttpContext.Response.Headers.RetryAfter = retryAfterSeconds.ToString();
        context.HttpContext.Response.ContentType = "application/json";
        await context.HttpContext.Response.WriteAsJsonAsync(
            new
            {
                message = "Too many requests. Please wait a moment and try again.",
                retryAfterSeconds
            },
            cancellationToken);
    };

    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
    {
        if (context.Request.Path.StartsWithSegments("/health", StringComparison.OrdinalIgnoreCase))
        {
            return RateLimitPartition.GetNoLimiter("health");
        }

        var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetFixedWindowLimiter(
            $"global:{ip}",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = globalLimit,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            });
    });

    options.AddPolicy(RateLimitPolicies.Auth, context =>
    {
        var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetFixedWindowLimiter(
            $"auth:{ip}",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = authLimit,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            });
    });
});

// Configure liveness and readiness health checks.
builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("database", tags: ["ready"]);

// Register controllers and OpenAPI.
// Register MVC services with automatic antiforgery validation.
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(
        new AutoValidateAntiforgeryTokenAttribute());
});
// Register Swagger/OpenAPI documentation.
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "PestiTrack API",
        Version = "v1",
        Description = "PestiTrack pesticide application management API."
    });

    options.OperationFilter<CsrfHeaderOperationFilter>();
});

var app = builder.Build();

// Log the resolved provider once at startup: on Render this is the
// first thing to check in the logs when the database is unreachable.
app.Logger.LogInformation(
    "PestiTrack database provider: {Provider}", databaseProvider);

// Handle explicit EF Core database migration command for containers / CI.
if (args.Contains(
    "--migrate",
    StringComparer.OrdinalIgnoreCase))
{
    await using var scope = app.Services.CreateAsyncScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
    Console.WriteLine("Database migrations applied successfully.");
    return;
}

// Handle one-time administrator provisioning.
if (args.Contains(
    "--bootstrap-admin",
    StringComparer.OrdinalIgnoreCase))
{
    await using var scope = app.Services.CreateAsyncScope();

    var bootstrapper = scope.ServiceProvider
        .GetRequiredService<AdminBootstrapper>();

    var exitCode = await bootstrapper.RunAsync();

    Environment.ExitCode = exitCode;
    return;
}

app.UseForwardedHeaders();
app.UseExceptionHandler();
app.UseMiddleware<SecurityHeadersMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();

    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint(
            "/swagger/v1/swagger.json",
            "PestiTrack API v1");
    });
}
else
{
    app.UseHsts();
}

if (!terminateTlsExternally)
{
    app.UseHttpsRedirection();
}

if (allowedOrigins.Length > 0)
{
    app.UseCors("TrustedOrigins");
}

app.UseRateLimiter();

// Authentication must run before authorization.
app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false,
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(new
        {
            status = report.Status.ToString(),
            timestampUtc = DateTimeOffset.UtcNow
        });
    }
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(new
        {
            status = report.Status.ToString(),
            timestampUtc = DateTimeOffset.UtcNow,
            checks = report.Entries.Select(entry => new
            {
                name = entry.Key,
                status = entry.Value.Status.ToString(),
                description = entry.Value.Description
            })
        });
    }
});

app.MapControllers();

app.Run();

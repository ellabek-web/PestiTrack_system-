
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PestiTrackBackend.Data;
using PestiTrackBackend.Models.Identity;
using PestiTrackBackend.Security.Bootstrap;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi;
using PestiTrackBackend.Swagger;
using PestiTrackBackend.Services.Authorization;
using PestiTrackBackend.Security.Authorization;
using Microsoft.AspNetCore.Authorization;



var builder = WebApplication.CreateBuilder(args);

// Register Entity Framework Core with SQL Server.
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")
    )
);

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
    .AddSignInManager();

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

// Register HTTP context access for authorization handlers.
builder.Services.AddHttpContextAccessor();

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

app.UseHttpsRedirection();

// Authentication must run before authorization.
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();


using Microsoft.EntityFrameworkCore;
using PestiTrackBackend.Data;
using PestiTrackBackend.Models.Identity;
using PestiTrackBackend.Security.Bootstrap;

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

        options.User.RequireUniqueEmail = true;

        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan =
            TimeSpan.FromMinutes(15);
        options.Lockout.AllowedForNewUsers = true;
    })
    .AddRoles<ApplicationRole>()
    .AddEntityFrameworkStores<AppDbContext>();

builder.Services.AddScoped<AdminBootstrapper>();

builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

// Handle one-time administrator provisioning.
if (args.Contains("--bootstrap-admin",
    StringComparer.OrdinalIgnoreCase))
{
    await using var scope = app.Services.CreateAsyncScope();

    var bootstrapper = scope.ServiceProvider
        .GetRequiredService<AdminBootstrapper>();

    var exitCode = await bootstrapper.RunAsync();

    Environment.ExitCode = exitCode;
    return;
}

// Normal API startup.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

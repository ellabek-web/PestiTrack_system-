
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PestiTrackBackend.Data;
using PestiTrackBackend.Models.Identity;
using PestiTrackBackend.Security;
using System.Data;

namespace PestiTrackBackend.Security.Bootstrap;

public sealed class AdminBootstrapper
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly AppDbContext _db;

    public AdminBootstrapper(
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        AppDbContext db)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _db = db;
    }


    public async Task<int> RunAsync()
    {
        var adminRole = await _roleManager.FindByNameAsync(
            SystemRoleCodes.Administrator);

        if (adminRole is null || !adminRole.IsSystemRole)
        {
            Console.Error.WriteLine(
                "Bootstrap failed: protected administrator role is missing.");

            return 1;
        }

        // Quick check before prompting for credentials.
        var existingAdmin = await _db.UserRoles
            .AnyAsync(ur => ur.RoleId == adminRole.Id);

        if (existingAdmin)
        {
            Console.Error.WriteLine(
                "Bootstrap refused: a system administrator already exists.");

            return 1;
        }

        Console.Write("Administrator display name: ");
        var displayName = Console.ReadLine()?.Trim();

        Console.Write("Administrator email: ");
        var email = Console.ReadLine()?.Trim();

        if (string.IsNullOrWhiteSpace(displayName) ||
            displayName.Length > 150 ||
            string.IsNullOrWhiteSpace(email))
        {
            Console.Error.WriteLine("Invalid display name or email.");
            return 1;
        }

        Console.Write("Administrator password: ");
        var password = ReadPassword();

        Console.WriteLine();

        Console.Write("Confirm password: ");
        var confirmation = ReadPassword();

        Console.WriteLine();

       if (password.Length < 8 ||
    password.Any(c => !char.IsDigit(c)) ||
    password != confirmation)
{
    Console.Error.WriteLine(
        "Password must contain at least 8 digits, and both passwords must match.");

    return 1;
}

        // Recheck and create within one serializable transaction.
        await using var transaction =
            await _db.Database.BeginTransactionAsync(
                IsolationLevel.Serializable);

        existingAdmin = await _db.UserRoles
            .AnyAsync(ur => ur.RoleId == adminRole.Id);

        if (existingAdmin)
        {
            Console.Error.WriteLine(
                "Bootstrap refused: another administrator was created.");

            return 1;
        }

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email,
            DisplayName = displayName,
            IsActive = true,
            EmailConfirmed = false
        };

        var createResult = await _userManager.CreateAsync(user, password);

        if (!createResult.Succeeded)
        {
            foreach (var error in createResult.Errors)
            {
                Console.Error.WriteLine(
                    $"{error.Code}: {error.Description}");
            }

            return 1;
        }

        var roleResult = await _userManager.AddToRoleAsync(
            user,
            SystemRoleCodes.Administrator);

        if (!roleResult.Succeeded)
        {
            foreach (var error in roleResult.Errors)
            {
                Console.Error.WriteLine(
                    $"{error.Code}: {error.Description}");
            }

            return 1;
        }

        await transaction.CommitAsync();

        Console.WriteLine();
        Console.WriteLine("Administrator account created successfully.");
        Console.WriteLine($"User ID: {user.Id}");
        Console.WriteLine($"Email: {user.Email}");
        Console.WriteLine("System role: SystemAdministrator");

        return 0;
    }


    private static string ReadPassword()
    {
        var password = new System.Text.StringBuilder();

        while (true)
        {
            var key = Console.ReadKey(intercept: true);

            if (key.Key == ConsoleKey.Enter)
                break;

            if (key.Key == ConsoleKey.Backspace)
            {
                if (password.Length > 0)
                    password.Length--;

                continue;
            }

            if (!char.IsControl(key.KeyChar))
                password.Append(key.KeyChar);
        }

        return password.ToString();
    }
}

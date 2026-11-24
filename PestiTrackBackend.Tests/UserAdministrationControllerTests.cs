using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PestiTrackBackend.Controllers;
using PestiTrackBackend.Data;
using PestiTrackBackend.DTOs.Users;
using PestiTrackBackend.Models.Identity;
using PestiTrackBackend.Security;

namespace PestiTrackBackend.Tests;

public sealed class UserAdministrationControllerTests
{
    [Fact]
    public async Task Create_ProvisionsUserAndAssignsSystemRole()
    {
        await using var fixture =
            await IdentityFixture.CreateAsync();

        var result = await fixture.Controller.Create(
            new CreateUserRequest
            {
                DisplayName = "Audit User",
                Email = "audit@example.com",
                Password = "password1",
                SystemRoles = [SystemRoleCodes.Auditor]
            },
            CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(
            result.Result);
        var response = Assert.IsType<UserResponse>(
            created.Value);
        var user = await fixture.UserManager.FindByIdAsync(
            response.Id.ToString());

        Assert.NotNull(user);
        Assert.True(
            await fixture.UserManager.CheckPasswordAsync(
                user!,
                "password1"));
        Assert.True(
            await fixture.UserManager.IsInRoleAsync(
                user!,
                SystemRoleCodes.Auditor));
    }

    [Fact]
    public async Task UpdateStatus_PreventsDisablingLastActiveAdministrator()
    {
        await using var fixture =
            await IdentityFixture.CreateAsync();
        var admin = await fixture.CreateUser(
            "Only Admin",
            "admin@example.com",
            SystemRoleCodes.Administrator);

        var result = await fixture.Controller.UpdateStatus(
            admin.Id,
            new UpdateUserStatusRequest
            {
                IsActive = false
            },
            CancellationToken.None);

        var conflict = Assert.IsType<ConflictObjectResult>(result);
        Assert.Equal(StatusCodes.Status409Conflict, conflict.StatusCode);

        var reloaded =
            await fixture.UserManager.FindByIdAsync(
                admin.Id.ToString());
        Assert.True(reloaded!.IsActive);
    }

    [Fact]
    public async Task PasswordResetAndUnlock_RecoversAccount()
    {
        await using var fixture =
            await IdentityFixture.CreateAsync();
        var user = await fixture.CreateUser(
            "Locked User",
            "locked@example.com");
        await fixture.UserManager.AccessFailedAsync(user);
        await fixture.UserManager.SetLockoutEndDateAsync(
            user,
            DateTimeOffset.UtcNow.AddHours(1));

        var resetResult =
            await fixture.Controller.ResetPassword(
                user.Id,
                new AdminResetPasswordRequest
                {
                    NewPassword = "newpassword1"
                });
        var unlockResult =
            await fixture.Controller.Unlock(user.Id);

        Assert.IsType<OkObjectResult>(resetResult);
        Assert.IsType<OkObjectResult>(unlockResult);

        var reloaded =
            await fixture.UserManager.FindByIdAsync(
                user.Id.ToString());
        Assert.NotNull(reloaded);
        Assert.True(
            await fixture.UserManager.CheckPasswordAsync(
                reloaded!,
                "newpassword1"));
        Assert.Equal(0, reloaded!.AccessFailedCount);
        Assert.False(
            await fixture.UserManager.IsLockedOutAsync(
                reloaded));
    }

    private sealed class IdentityFixture : IAsyncDisposable
    {
        private readonly ServiceProvider _provider;
        private readonly AsyncServiceScope _scope;

        private IdentityFixture(
            ServiceProvider provider,
            AsyncServiceScope scope,
            AppDbContext db,
            UserManager<ApplicationUser> userManager,
            UserAdministrationController controller)
        {
            _provider = provider;
            _scope = scope;
            Db = db;
            UserManager = userManager;
            Controller = controller;
        }

        public AppDbContext Db { get; }

        public UserManager<ApplicationUser> UserManager { get; }

        public UserAdministrationController Controller { get; }

        public static async Task<IdentityFixture> CreateAsync()
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddDataProtection();
            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase(
                    Guid.NewGuid().ToString()));
            services
                .AddIdentityCore<ApplicationUser>(options =>
                {
                    options.Password.RequiredLength = 8;
                    options.Password.RequireDigit = true;
                    options.Password.RequireLowercase = false;
                    options.Password.RequireUppercase = false;
                    options.Password.RequireNonAlphanumeric = false;
                    options.User.RequireUniqueEmail = true;
                })
                .AddRoles<ApplicationRole>()
                .AddEntityFrameworkStores<AppDbContext>()
                .AddDefaultTokenProviders();

            var provider = services.BuildServiceProvider();
            var scope = provider.CreateAsyncScope();
            var db =
                scope.ServiceProvider
                    .GetRequiredService<AppDbContext>();
            await db.Database.EnsureCreatedAsync();
            var userManager =
                scope.ServiceProvider
                    .GetRequiredService<
                        UserManager<ApplicationUser>>();
            var controller =
                new UserAdministrationController(
                    db,
                    userManager,
                    TimeProvider.System)
                {
                    ControllerContext = new ControllerContext
                    {
                        HttpContext = new DefaultHttpContext
                        {
                            User = new ClaimsPrincipal(
                                new ClaimsIdentity(
                                    [
                                        new Claim(
                                            ClaimTypes.NameIdentifier,
                                            Guid.NewGuid().ToString()),
                                        new Claim(
                                            ClaimTypes.Role,
                                            SystemRoleCodes.Administrator)
                                    ],
                                    "Test"))
                        }
                    }
                };

            return new IdentityFixture(
                provider,
                scope,
                db,
                userManager,
                controller);
        }

        public async Task<ApplicationUser> CreateUser(
            string displayName,
            string email,
            string? role = null)
        {
            var user = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = email,
                Email = email,
                DisplayName = displayName,
                IsActive = true
            };
            var result = await UserManager.CreateAsync(
                user,
                "password1");
            Assert.True(
                result.Succeeded,
                string.Join(
                    ", ",
                    result.Errors.Select(error =>
                        error.Description)));

            if (role is not null)
            {
                var roleResult =
                    await UserManager.AddToRoleAsync(
                        user,
                        role);
                Assert.True(roleResult.Succeeded);
            }

            return user;
        }

        public async ValueTask DisposeAsync()
        {
            await _scope.DisposeAsync();
            await _provider.DisposeAsync();
        }
    }
}

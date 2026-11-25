using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PestiTrackBackend.Data;
using PestiTrackBackend.Models.Authorization;
using PestiTrackBackend.Models.Farms;
using PestiTrackBackend.Models.Identity;
using PestiTrackBackend.Security;
using PestiTrackBackend.Services.Authorization;

namespace PestiTrackBackend.Tests;

public sealed class FarmAuthorizationServiceTests
{
    [Fact]
    public async Task Administrator_HasFarmPermissionWithoutAssignment()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var db = new AppDbContext(options);
        var user = new ApplicationUser
        {
            DisplayName = "Administrator",
            UserName = "admin@example.com",
            Email = "admin@example.com",
            IsActive = true
        };
        var role = new ApplicationRole
        {
            Name = SystemRoleCodes.Administrator,
            NormalizedName =
                SystemRoleCodes.Administrator.ToUpperInvariant()
        };
        var farm = new Farm
        {
            Name = "Unassigned Farm",
            Code = "UNASSIGNED",
            IsActive = false
        };
        db.AddRange(user, role, farm);
        db.UserRoles.Add(new IdentityUserRole<Guid>
        {
            UserId = user.Id,
            RoleId = role.Id
        });
        await db.SaveChangesAsync();

        var service = new FarmAuthorizationService(db);
        var allowed = await service.HasPermissionAsync(
            user.Id,
            farm.Id,
            PermissionCodes.ReportsView);

        Assert.True(allowed);
    }

    [Fact]
    public async Task OrdinaryUser_StillRequiresFarmRolePermission()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var db = new AppDbContext(options);
        var user = new ApplicationUser
        {
            DisplayName = "Farm User",
            UserName = "user@example.com",
            Email = "user@example.com",
            IsActive = true
        };
        var farm = new Farm
        {
            Name = "Farm",
            Code = "FARM"
        };
        db.AddRange(user, farm);
        await db.SaveChangesAsync();

        var service = new FarmAuthorizationService(db);
        var allowed = await service.HasPermissionAsync(
            user.Id,
            farm.Id,
            PermissionCodes.ReportsView);

        Assert.False(allowed);
    }

    [Fact]
    public async Task Administrator_GetsFullFarmPermissionCatalog()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var db = new AppDbContext(options);
        var user = new ApplicationUser
        {
            DisplayName = "Administrator",
            UserName = "admin@example.com",
            Email = "admin@example.com",
            IsActive = true
        };
        var role = new ApplicationRole
        {
            Name = SystemRoleCodes.Administrator,
            NormalizedName =
                SystemRoleCodes.Administrator.ToUpperInvariant()
        };
        var farm = new Farm { Name = "Farm", Code = "FARM" };
        db.AddRange(user, role, farm);
        db.UserRoles.Add(new IdentityUserRole<Guid>
        {
            UserId = user.Id,
            RoleId = role.Id
        });
        await db.SaveChangesAsync();

        var service = new FarmAuthorizationService(db);
        var permissions = await service.GetPermissionsAsync(
            user.Id,
            farm.Id);

        Assert.Equal(PermissionSeedData.All.Length, permissions.Count);
        Assert.Contains(PermissionCodes.ReportsExport, permissions);
        Assert.Contains(PermissionCodes.AuditView, permissions);
    }

    [Fact]
    public async Task AssignedFarmUser_GetsOnlyAssignedRolePermissions()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var db = new AppDbContext(options);
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            DisplayName = "Operator",
            UserName = "operator@example.com",
            Email = "operator@example.com",
            IsActive = true
        };
        var farm = new Farm { Name = "Farm", Code = "FARM" };
        var permView = new Permission
        {
            Code = PermissionCodes.ApplicationsView,
            Name = "View applications"
        };
        var permCreate = new Permission
        {
            Code = PermissionCodes.ApplicationsCreate,
            Name = "Create applications"
        };
        var farmRole = new FarmRole
        {
            FarmId = farm.Id,
            Name = "Field Operator"
        };
        var farmUser = new FarmUser
        {
            FarmId = farm.Id,
            UserId = user.Id,
            IsActive = true
        };
        db.AddRange(user, farm, permView, permCreate, farmRole, farmUser);
        db.FarmRolePermissions.AddRange(
            new FarmRolePermission
            {
                FarmRoleId = farmRole.Id,
                PermissionId = permView.Id
            },
            new FarmRolePermission
            {
                FarmRoleId = farmRole.Id,
                PermissionId = permCreate.Id
            });
        db.FarmUserRoles.Add(new FarmUserRole
        {
            FarmId = farm.Id,
            FarmUserId = farmUser.Id,
            FarmRoleId = farmRole.Id
        });
        await db.SaveChangesAsync();

        var service = new FarmAuthorizationService(db);
        var permissions = await service.GetPermissionsAsync(
            user.Id,
            farm.Id);

        Assert.Equal(
            [PermissionCodes.ApplicationsCreate, PermissionCodes.ApplicationsView],
            permissions);
    }
}


using Microsoft.EntityFrameworkCore;
using PestiTrackBackend.Data;
using PestiTrackBackend.Security;

namespace PestiTrackBackend.Services.Authorization;

public sealed class FarmAuthorizationService
    : IFarmAuthorizationService
{
    private readonly AppDbContext _db;

    public FarmAuthorizationService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<bool> HasPermissionAsync(
        Guid userId,
        Guid farmId,
        string permissionCode,
        CancellationToken cancellationToken = default)
    {
        // Reject invalid authorization inputs.
        if (userId == Guid.Empty ||
            farmId == Guid.Empty ||
            string.IsNullOrWhiteSpace(permissionCode))
        {
            return false;
        }

        // System administrators manage every farm and do not need a
        // separate farm-role assignment for each permission.
        if (await IsSystemAdministratorAsync(userId, cancellationToken))
            return true;

        // Verify the user, farm, assignment, and permission
        // in one database query.
        return await _db.FarmUsers
            .AsNoTracking()
            .AnyAsync(
                farmUser =>
                    farmUser.UserId == userId &&
                    farmUser.FarmId == farmId &&
                    farmUser.IsActive &&
                    farmUser.User.IsActive &&
                    farmUser.Farm.IsActive &&
                    farmUser.Roles.Any(
                        userRole =>
                            userRole.FarmRole.Permissions.Any(
                                rolePermission =>
                                    rolePermission.Permission.Code ==
                                    permissionCode)),
                cancellationToken);
    }

    public async Task<IReadOnlyList<string>> GetPermissionsAsync(
        Guid userId,
        Guid farmId,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty || farmId == Guid.Empty)
        {
            return [];
        }

        if (await IsSystemAdministratorAsync(userId, cancellationToken))
        {
            return PermissionSeedData.All
                .Select(permission => permission.Code)
                .OrderBy(code => code, StringComparer.Ordinal)
                .ToList();
        }

        return await _db.FarmUserRoles
            .AsNoTracking()
            .Where(userRole =>
                userRole.FarmId == farmId &&
                userRole.FarmUser.UserId == userId &&
                userRole.FarmUser.IsActive &&
                userRole.FarmUser.User.IsActive &&
                userRole.FarmUser.Farm.IsActive)
            .Join(
                _db.FarmRolePermissions,
                userRole => userRole.FarmRoleId,
                rolePermission => rolePermission.FarmRoleId,
                (_, rolePermission) => rolePermission.Permission.Code)
            .Distinct()
            .OrderBy(code => code)
            .ToListAsync(cancellationToken);
    }

    private Task<bool> IsSystemAdministratorAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        return _db.Users
            .AsNoTracking()
            .Where(user =>
                user.Id == userId &&
                user.IsActive)
            .AnyAsync(
                user => _db.UserRoles
                    .Where(userRole =>
                        userRole.UserId == user.Id)
                    .Join(
                        _db.Roles,
                        userRole => userRole.RoleId,
                        role => role.Id,
                        (_, role) => role.Name)
                    .Any(roleName =>
                        roleName ==
                            SystemRoleCodes.Administrator),
                cancellationToken);
    }
}

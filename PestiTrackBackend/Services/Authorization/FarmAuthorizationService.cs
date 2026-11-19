
using Microsoft.EntityFrameworkCore;
using PestiTrackBackend.Data;

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
}

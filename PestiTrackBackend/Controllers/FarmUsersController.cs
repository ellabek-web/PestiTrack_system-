using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PestiTrackBackend.Data;
using PestiTrackBackend.DTOs.FarmUsers;
using PestiTrackBackend.Models.Farms;
using PestiTrackBackend.Security;

namespace PestiTrackBackend.Controllers;

[ApiController]
[Route("api/farms/{farmId:guid}/users")]
[Authorize(Roles = SystemRoleCodes.Administrator)]
public sealed class FarmUsersController : ControllerBase
{
    private readonly AppDbContext _db;

    public FarmUsersController(AppDbContext db)
    {
        _db = db;
    }

    // GET: api/farms/{farmId}/users
    [HttpGet]
    public async Task<ActionResult<List<FarmUserResponse>>> GetUsers(
        Guid farmId,
        CancellationToken cancellationToken)
    {
        var farmExists = await _db.Farms
            .AnyAsync(
                farm => farm.Id == farmId,
                cancellationToken);

        if (!farmExists)
        {
            return NotFound(new
            {
                message = "Farm not found."
            });
        }

        var users = await _db.FarmUsers
            .AsNoTracking()
            .Where(assignment => assignment.FarmId == farmId)
            .OrderBy(assignment => assignment.User.DisplayName)
            .Select(assignment => new FarmUserResponse
            {
                Id = assignment.Id,
                FarmId = assignment.FarmId,
                UserId = assignment.UserId,
                DisplayName = assignment.User.DisplayName,
                Email = assignment.User.Email,
                IsActive = assignment.IsActive,
                AssignedAtUtc = assignment.AssignedAtUtc,
                Roles = assignment.Roles
                    .Select(userRole => new FarmUserRoleResponse
                    {
                        Id = userRole.FarmRoleId,
                        Name = userRole.FarmRole.Name
                    })
                    .ToList()
            })
            .ToListAsync(cancellationToken);

        return Ok(users);
    }

    // POST: api/farms/{farmId}/users
    [HttpPost]
    public async Task<ActionResult<FarmUserResponse>> AssignUser(
        Guid farmId,
        AssignFarmUserRequest request,
        CancellationToken cancellationToken)
    {
        var farm = await _db.Farms
            .FirstOrDefaultAsync(
                farm => farm.Id == farmId,
                cancellationToken);

        if (farm is null)
        {
            return NotFound(new
            {
                message = "Farm not found."
            });
        }

        if (!farm.IsActive)
        {
            return BadRequest(new
            {
                message = "Cannot assign users to an inactive farm."
            });
        }

        var user = await _db.Users
            .FirstOrDefaultAsync(
                user => user.Id == request.UserId,
                cancellationToken);

        if (user is null)
        {
            return NotFound(new
            {
                message = "User not found."
            });
        }

        if (!user.IsActive)
        {
            return BadRequest(new
            {
                message = "Cannot assign an inactive user."
            });
        }

        var roleIds = request.FarmRoleIds
            .Distinct()
            .ToList();

        if (roleIds.Count == 0 ||
            roleIds.Count != request.FarmRoleIds.Count ||
            roleIds.Any(id => id == Guid.Empty))
        {
            return BadRequest(new
            {
                message = "Provide valid, unique farm role IDs."
            });
        }

        var roles = await _db.FarmRoles
            .Where(role =>
                role.FarmId == farmId &&
                roleIds.Contains(role.Id) &&
                !role.IsSystemRole)
            .ToListAsync(cancellationToken);

        if (roles.Count != roleIds.Count)
        {
            return BadRequest(new
            {
                message = "One or more roles are invalid or do not belong to this farm."
            });
        }

        var existingAssignment = await _db.FarmUsers
            .Include(assignment => assignment.Roles)
            .FirstOrDefaultAsync(
                assignment =>
                    assignment.FarmId == farmId &&
                    assignment.UserId == request.UserId,
                cancellationToken);

        if (existingAssignment is not null &&
            existingAssignment.IsActive)
        {
            return Conflict(new
            {
                message = "This user is already assigned to the farm."
            });
        }

        FarmUser assignment;

        if (existingAssignment is not null)
        {
            // Reactivate the existing assignment.
            assignment = existingAssignment;
            assignment.IsActive = true;

            // Replace the previous role assignments.
            _db.FarmUserRoles.RemoveRange(assignment.Roles);
            assignment.Roles.Clear();
        }
        else
        {
            assignment = new FarmUser
            {
                FarmId = farmId,
                UserId = user.Id,
                IsActive = true,
                AssignedAtUtc = DateTime.UtcNow
            };

            _db.FarmUsers.Add(assignment);
        }

        foreach (var role in roles)
        {
            assignment.Roles.Add(new FarmUserRole
            {
                FarmId = farmId,
                FarmRoleId = role.Id
            });
        }

        await _db.SaveChangesAsync(cancellationToken);

        var response = new FarmUserResponse
        {
            Id = assignment.Id,
            FarmId = assignment.FarmId,
            UserId = user.Id,
            DisplayName = user.DisplayName,
            Email = user.Email,
            IsActive = assignment.IsActive,
            AssignedAtUtc = assignment.AssignedAtUtc,
            Roles = roles.Select(role => new FarmUserRoleResponse
            {
                Id = role.Id,
                Name = role.Name
            }).ToList()
        };

        return Ok(response);
    }

    // PUT: api/farms/{farmId}/users/{farmUserId}/roles
    [HttpPut("{farmUserId:guid}/roles")]
    public async Task<ActionResult<FarmUserResponse>> UpdateRoles(
        Guid farmId,
        Guid farmUserId,
        UpdateFarmUserRolesRequest request,
        CancellationToken cancellationToken)
    {
        var assignment = await _db.FarmUsers
            .Include(item => item.User)
            .Include(item => item.Roles)
            .FirstOrDefaultAsync(
                item =>
                    item.Id == farmUserId &&
                    item.FarmId == farmId,
                cancellationToken);

        if (assignment is null)
        {
            return NotFound(new
            {
                message = "Farm user assignment not found."
            });
        }

        var roleIds = request.FarmRoleIds
            .Distinct()
            .ToList();

        if (roleIds.Count == 0 ||
            roleIds.Count != request.FarmRoleIds.Count ||
            roleIds.Any(id => id == Guid.Empty))
        {
            return BadRequest(new
            {
                message = "Provide valid, unique farm role IDs."
            });
        }

        var roles = await _db.FarmRoles
            .Where(role =>
                role.FarmId == farmId &&
                roleIds.Contains(role.Id) &&
                !role.IsSystemRole)
            .ToListAsync(cancellationToken);

        if (roles.Count != roleIds.Count)
        {
            return BadRequest(new
            {
                message = "One or more roles are invalid or do not belong to this farm."
            });
        }

        _db.FarmUserRoles.RemoveRange(assignment.Roles);
        assignment.Roles.Clear();

        foreach (var role in roles)
        {
            assignment.Roles.Add(new FarmUserRole
            {
                FarmId = farmId,
                FarmUserId = assignment.Id,
                FarmRoleId = role.Id
            });
        }

        await _db.SaveChangesAsync(cancellationToken);

        return Ok(new FarmUserResponse
        {
            Id = assignment.Id,
            FarmId = assignment.FarmId,
            UserId = assignment.UserId,
            DisplayName = assignment.User.DisplayName,
            Email = assignment.User.Email,
            IsActive = assignment.IsActive,
            AssignedAtUtc = assignment.AssignedAtUtc,
            Roles = roles.Select(role => new FarmUserRoleResponse
            {
                Id = role.Id,
                Name = role.Name
            }).ToList()
        });
    }

    // PATCH: api/farms/{farmId}/users/{farmUserId}/status
    [HttpPatch("{farmUserId:guid}/status")]
    public async Task<IActionResult> UpdateStatus(
        Guid farmId,
        Guid farmUserId,
        UpdateFarmUserStatusRequest request,
        CancellationToken cancellationToken)
    {
        var assignment = await _db.FarmUsers
            .FirstOrDefaultAsync(
                item =>
                    item.Id == farmUserId &&
                    item.FarmId == farmId,
                cancellationToken);

        if (assignment is null)
        {
            return NotFound(new
            {
                message = "Farm user assignment not found."
            });
        }

        assignment.IsActive = request.IsActive;

        await _db.SaveChangesAsync(cancellationToken);

        return Ok(new
        {
            message = request.IsActive
                ? "Farm user assignment activated successfully."
                : "Farm user assignment deactivated successfully.",
            farmUserId = assignment.Id,
            isActive = assignment.IsActive
        });
    }
}
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PestiTrackBackend.Data;
using PestiTrackBackend.DTOs.FarmRoles;
using PestiTrackBackend.Models.Authorization;
using PestiTrackBackend.Security;

namespace PestiTrackBackend.Controllers;

[ApiController]
[Route("api/farms/{farmId:guid}/roles")]
[Authorize(Roles = SystemRoleCodes.Administrator)]
public sealed class FarmRolesController : ControllerBase
{
    private readonly AppDbContext _db;

    public FarmRolesController(AppDbContext db)
    {
        _db = db;
    }

    // GET: api/farms/{farmId}/roles
    [HttpGet]
    public async Task<ActionResult<List<FarmRoleResponse>>> GetRoles(
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

        var roles = await _db.FarmRoles
            .AsNoTracking()
            .Where(role => role.FarmId == farmId)
            .OrderBy(role => role.Name)
            .Select(role => new FarmRoleResponse
            {
                Id = role.Id,
                FarmId = role.FarmId,
                Name = role.Name,
                Description = role.Description,
                IsSystemRole = role.IsSystemRole,
                PermissionCodes = role.Permissions
                    .Select(permission => permission.Permission.Code)
                    .ToList()
            })
            .ToListAsync(cancellationToken);

        return Ok(roles);
    }

    // POST: api/farms/{farmId}/roles
    [HttpPost]
    public async Task<ActionResult<FarmRoleResponse>> CreateRole(
        Guid farmId,
        CreateFarmRoleRequest request,
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

        var name = request.Name.Trim();

        var roleExists = await _db.FarmRoles
            .AnyAsync(
                role =>
                    role.FarmId == farmId &&
                    role.Name == name,
                cancellationToken);

        if (roleExists)
        {
            return Conflict(new
            {
                message = "A role with this name already exists in this farm."
            });
        }

        var permissionCodes = request.PermissionCodes
            .Where(code => !string.IsNullOrWhiteSpace(code))
            .Select(code => code.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (permissionCodes.Count == 0 ||
            permissionCodes.Count != request.PermissionCodes.Count)
        {
            return BadRequest(new
            {
                message = "Provide a non-empty list of unique permission codes."
            });
        }

        var permissions = await _db.Permissions
            .Where(permission =>
                permissionCodes.Contains(permission.Code))
            .ToListAsync(cancellationToken);

        if (permissions.Count != permissionCodes.Count)
        {
            return BadRequest(new
            {
                message = "One or more permission codes are invalid.",
                suppliedPermissionCodes = permissionCodes
            });
        }

        var role = new FarmRole
        {
            FarmId = farmId,
            Name = name,
            Description = string.IsNullOrWhiteSpace(request.Description)
                ? null
                : request.Description.Trim(),
            IsSystemRole = false
        };

        foreach (var permission in permissions)
        {
            role.Permissions.Add(new FarmRolePermission
            {
                PermissionId = permission.Id
            });
        }

        _db.FarmRoles.Add(role);

        await _db.SaveChangesAsync(cancellationToken);

        var response = new FarmRoleResponse
        {
            Id = role.Id,
            FarmId = role.FarmId,
            Name = role.Name,
            Description = role.Description,
            IsSystemRole = role.IsSystemRole,
            PermissionCodes = permissions
                .Select(permission => permission.Code)
                .ToList()
        };

        return CreatedAtAction(
            nameof(GetRoles),
            new { farmId },
            response);
    }

    // PUT: api/farms/{farmId}/roles/{roleId}
    [HttpPut("{roleId:guid}")]
    public async Task<ActionResult<FarmRoleResponse>> UpdateRole(
        Guid farmId,
        Guid roleId,
        UpdateFarmRoleRequest request,
        CancellationToken cancellationToken)
    {
        var role = await _db.FarmRoles
            .Include(role => role.Permissions)
            .FirstOrDefaultAsync(
                role =>
                    role.Id == roleId &&
                    role.FarmId == farmId,
                cancellationToken);

        if (role is null)
        {
            return NotFound(new
            {
                message = "Farm role not found."
            });
        }

        if (role.IsSystemRole)
        {
            return Forbid();
        }

        var name = request.Name.Trim();

        var duplicateName = await _db.FarmRoles
            .AnyAsync(
                existing =>
                    existing.FarmId == farmId &&
                    existing.Id != roleId &&
                    existing.Name == name,
                cancellationToken);

        if (duplicateName)
        {
            return Conflict(new
            {
                message = "Another role already uses this name."
            });
        }

        var permissionCodes = request.PermissionCodes
            .Where(code => !string.IsNullOrWhiteSpace(code))
            .Select(code => code.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (permissionCodes.Count == 0 ||
            permissionCodes.Count != request.PermissionCodes.Count)
        {
            return BadRequest(new
            {
                message = "Provide a non-empty list of unique permission codes."
            });
        }

        var permissions = await _db.Permissions
            .Where(permission =>
                permissionCodes.Contains(permission.Code))
            .ToListAsync(cancellationToken);

        if (permissions.Count != permissionCodes.Count)
        {
            return BadRequest(new
            {
                message = "One or more permission codes are invalid."
            });
        }

        role.Name = name;
        role.Description = string.IsNullOrWhiteSpace(request.Description)
            ? null
            : request.Description.Trim();

        _db.FarmRolePermissions.RemoveRange(role.Permissions);

        role.Permissions.Clear();

        foreach (var permission in permissions)
        {
            role.Permissions.Add(new FarmRolePermission
            {
                FarmRoleId = role.Id,
                PermissionId = permission.Id
            });
        }

        await _db.SaveChangesAsync(cancellationToken);

        return Ok(new FarmRoleResponse
        {
            Id = role.Id,
            FarmId = role.FarmId,
            Name = role.Name,
            Description = role.Description,
            IsSystemRole = role.IsSystemRole,
            PermissionCodes = permissions
                .Select(permission => permission.Code)
                .ToList()
        });
    }
}
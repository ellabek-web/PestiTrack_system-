using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PestiTrackBackend.Data;
using PestiTrackBackend.DTOs.Farms;
using PestiTrackBackend.Models.Farms;
using PestiTrackBackend.Security;
using PestiTrackBackend.Security.Authorization;
using PestiTrackBackend.Services.Authorization;

namespace PestiTrackBackend.Controllers;

[ApiController]
[Route("api/farms")]
[Authorize]
public sealed class FarmsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IFarmAuthorizationService _farmAuthorizationService;

    public FarmsController(
        AppDbContext db,
        IFarmAuthorizationService farmAuthorizationService)
    {
        _db = db;
        _farmAuthorizationService = farmAuthorizationService;
    }

    // GET: api/farms
    // System administrators see the farm list.
    // Other users see only farms where they have farms.view.
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<FarmResponse>>> GetAll(
        CancellationToken cancellationToken)
    {
        var userIdValue = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(userIdValue, out var userId))
        {
            return Unauthorized();
        }

        var query = _db.Farms
            .AsNoTracking()
            .Where(farm => farm.IsActive);

        if (!User.IsInRole(SystemRoleCodes.Administrator))
        {
            query = query.Where(farm =>
                farm.UserAssignments.Any(assignment =>
                    assignment.UserId == userId &&
                    assignment.IsActive &&
                    assignment.User.IsActive &&
                    assignment.Roles.Any(userRole =>
                        userRole.FarmRole.Permissions.Any(
                            rolePermission =>
                                rolePermission.Permission.Code ==
                                "farms.view"))));
        }

        var farms = await query
            .OrderBy(farm => farm.Name)
            .Select(farm => new FarmResponse
            {
                Id = farm.Id,
                Name = farm.Name,
                Code = farm.Code,
                Location = farm.Location,
                Latitude = farm.Latitude,
                Longitude = farm.Longitude,
                IsActive = farm.IsActive,
                CreatedAtUtc = farm.CreatedAtUtc
            })
            .ToListAsync(cancellationToken);

        return Ok(farms);
    }

    // GET: api/farms/{farmId}
    [HttpGet("{farmId:guid}")]
    [FarmPermission("farms.view")]
    public async Task<ActionResult<FarmResponse>> GetById(
        Guid farmId,
        CancellationToken cancellationToken)
    {
        var farm = await _db.Farms
            .AsNoTracking()
            .Where(farm =>
                farm.Id == farmId &&
                farm.IsActive)
            .Select(farm => new FarmResponse
            {
                Id = farm.Id,
                Name = farm.Name,
                Code = farm.Code,
                Location = farm.Location,
                Latitude = farm.Latitude,
                Longitude = farm.Longitude,
                IsActive = farm.IsActive,
                CreatedAtUtc = farm.CreatedAtUtc
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (farm is null)
        {
            return NotFound(new
            {
                message = "Farm not found."
            });
        }

        return Ok(farm);
    }

    // GET: api/farms/{farmId}/permissions
    [HttpGet("{farmId:guid}/permissions")]
    public async Task<IActionResult> GetMyPermissions(
        Guid farmId,
        CancellationToken cancellationToken)
    {
        var userIdValue = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(userIdValue, out var userId))
        {
            return Unauthorized();
        }

        var permissions = await _farmAuthorizationService
            .GetPermissionsAsync(userId, farmId, cancellationToken);

        return Ok(new
        {
            farmId,
            permissions
        });
    }

    // POST: api/farms
    // Only system administrators can create farms.
    [HttpPost]
    [Authorize(Roles = SystemRoleCodes.Administrator)]
    public async Task<ActionResult<FarmResponse>> Create(
        CreateFarmRequest request,
        CancellationToken cancellationToken)
    {
        var name = request.Name.Trim();
        var code = request.Code.Trim().ToUpperInvariant();
        var location = string.IsNullOrWhiteSpace(request.Location)
            ? null
            : request.Location.Trim();

        var codeExists = await _db.Farms
            .AnyAsync(
                farm => farm.Code == code,
                cancellationToken);

        if (codeExists)
        {
            return Conflict(new
            {
                message = "A farm with this code already exists."
            });
        }

        var farm = new Farm
        {
            Name = name,
            Code = code,
            Location = location,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        _db.Farms.Add(farm);

        await _db.SaveChangesAsync(cancellationToken);

        var response = new FarmResponse
        {
            Id = farm.Id,
            Name = farm.Name,
            Code = farm.Code,
            Location = farm.Location,
            Latitude = farm.Latitude,
            Longitude = farm.Longitude,
            IsActive = farm.IsActive,
            CreatedAtUtc = farm.CreatedAtUtc
        };

        return CreatedAtAction(
            nameof(GetById),
            new { farmId = farm.Id },
            response);
    }

    // PUT: api/farms/{farmId}
    [HttpPut("{farmId:guid}")]
    [FarmPermission("farms.manage")]
    public async Task<ActionResult<FarmResponse>> Update(
        Guid farmId,
        UpdateFarmRequest request,
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

        farm.Name = request.Name.Trim();
        farm.Location = string.IsNullOrWhiteSpace(request.Location)
            ? null
            : request.Location.Trim();
        farm.Latitude = request.Latitude;
        farm.Longitude = request.Longitude;

        await _db.SaveChangesAsync(cancellationToken);

        return Ok(new FarmResponse
        {
            Id = farm.Id,
            Name = farm.Name,
            Code = farm.Code,
            Location = farm.Location,
            Latitude = farm.Latitude,
            Longitude = farm.Longitude,
            IsActive = farm.IsActive,
            CreatedAtUtc = farm.CreatedAtUtc
        });
    }

    // PATCH: api/farms/{farmId}/status
    [HttpPatch("{farmId:guid}/status")]
    [FarmPermission("farms.manage")]
    public async Task<IActionResult> UpdateStatus(
        Guid farmId,
        UpdateFarmStatusRequest request,
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

        farm.IsActive = request.IsActive;

        await _db.SaveChangesAsync(cancellationToken);

        return Ok(new
        {
            message = request.IsActive
                ? "Farm activated successfully."
                : "Farm deactivated successfully.",
            farmId = farm.Id,
            isActive = farm.IsActive
        });
    }
}
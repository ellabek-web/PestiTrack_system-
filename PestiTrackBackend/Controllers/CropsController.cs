using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PestiTrackBackend.Data;
using PestiTrackBackend.DTOs.Crops;
using PestiTrackBackend.Models.Crops;
using PestiTrackBackend.Security;
using PestiTrackBackend.Security.Authorization;

namespace PestiTrackBackend.Controllers;

[ApiController]
[Authorize]
[Route("api/crops")]
public sealed class CropsController : ControllerBase
{
    private readonly AppDbContext _db;

    public CropsController(AppDbContext db)
    {
        _db = db;
    }

    // GET /api/farms/{farmId}/crops/catalog
    [HttpGet("/api/farms/{farmId:guid}/crops/catalog")]
    [FarmPermission(PermissionCodes.CropsView)]
    public async Task<IActionResult> GetFarmCropCatalog(
        Guid farmId,
        CancellationToken cancellationToken)
    {
        var farmExists = await _db.Farms
            .AnyAsync(
                f => f.Id == farmId && f.IsActive,
                cancellationToken);

        if (!farmExists)
        {
            return NotFound(new { message = "Active farm not found." });
        }

        var crops = await _db.Crops
            .AsNoTracking()
            .Where(c => c.IsActive)
            .OrderBy(c => c.Name)
            .Select(c => new CropResponse
            {
                Id = c.Id,
                Name = c.Name,
                ScientificName = c.ScientificName,
                Description = c.Description,
                IsActive = c.IsActive,
                CreatedAtUtc = c.CreatedAtUtc
            })
            .ToListAsync(cancellationToken);

        return Ok(crops);
    }

    // GET /api/crops/{cropId}
    [HttpGet("{cropId:guid}")]
    [Authorize(Roles = SystemRoleCodes.Administrator)]
    public async Task<IActionResult> GetById(
        Guid cropId,
        CancellationToken cancellationToken)
    {
        var crop = await _db.Crops
            .AsNoTracking()
            .FirstOrDefaultAsync(
                c => c.Id == cropId,
                cancellationToken);

        if (crop is null)
        {
            return NotFound(new { message = "Crop not found." });
        }

        return Ok(ToResponse(crop));
    }

    // POST /api/crops
    [HttpPost]
    [Authorize(Roles = SystemRoleCodes.Administrator)]
    public async Task<IActionResult> Create(
        [FromBody] CreateCropRequest request,
        CancellationToken cancellationToken)
    {
        var name = request.Name.Trim();

        var duplicate = await _db.Crops
            .AnyAsync(
                c => c.Name == name,
                cancellationToken);

        if (duplicate)
        {
            return Conflict(new
            {
                message = "A crop with this name already exists."
            });
        }

        var crop = new Crop
        {
            Name = name,
            ScientificName = CleanOptional(request.ScientificName),
            Description = CleanOptional(request.Description)
        };

        _db.Crops.Add(crop);

        await _db.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { cropId = crop.Id },
            ToResponse(crop));
    }

    // PUT /api/crops/{cropId}
    [HttpPut("{cropId:guid}")]
    [Authorize(Roles = SystemRoleCodes.Administrator)]
    public async Task<IActionResult> Update(
        Guid cropId,
        [FromBody] UpdateCropRequest request,
        CancellationToken cancellationToken)
    {
        var crop = await _db.Crops
            .FirstOrDefaultAsync(
                c => c.Id == cropId,
                cancellationToken);

        if (crop is null)
        {
            return NotFound(new { message = "Crop not found." });
        }

        var name = request.Name.Trim();

        var duplicate = await _db.Crops
            .AnyAsync(
                c => c.Id != cropId && c.Name == name,
                cancellationToken);

        if (duplicate)
        {
            return Conflict(new
            {
                message = "Another crop already uses this name."
            });
        }

        crop.Name = name;
        crop.ScientificName = CleanOptional(request.ScientificName);
        crop.Description = CleanOptional(request.Description);

        await _db.SaveChangesAsync(cancellationToken);

        return Ok(ToResponse(crop));
    }

    // PATCH /api/crops/{cropId}/status
    [HttpPatch("{cropId:guid}/status")]
    [Authorize(Roles = SystemRoleCodes.Administrator)]
    public async Task<IActionResult> UpdateStatus(
        Guid cropId,
        [FromBody] UpdateCropStatusRequest request,
        CancellationToken cancellationToken)
    {
        var crop = await _db.Crops
            .FirstOrDefaultAsync(
                c => c.Id == cropId,
                cancellationToken);

        if (crop is null)
        {
            return NotFound(new { message = "Crop not found." });
        }

        crop.IsActive = request.IsActive;

        await _db.SaveChangesAsync(cancellationToken);

        return Ok(new
        {
            message = request.IsActive
                ? "Crop activated successfully."
                : "Crop deactivated successfully.",
            cropId = crop.Id,
            isActive = crop.IsActive
        });
    }

    private static CropResponse ToResponse(Crop crop)
    {
        return new CropResponse
        {
            Id = crop.Id,
            Name = crop.Name,
            ScientificName = crop.ScientificName,
            Description = crop.Description,
            IsActive = crop.IsActive,
            CreatedAtUtc = crop.CreatedAtUtc
        };
    }

    private static string? CleanOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}
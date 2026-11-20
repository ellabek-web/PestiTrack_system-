
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
[Route("api/farms/{farmId:guid}/fields/{fieldId:guid}/crops")]
public sealed class FieldCropsController : ControllerBase
{
    private readonly AppDbContext _db;

    public FieldCropsController(AppDbContext db)
    {
        _db = db;
    }

    // GET /api/farms/{farmId}/fields/{fieldId}/crops
    [HttpGet]
    [FarmPermission(PermissionCodes.CropsView)]
    public async Task<IActionResult> GetAll(
        Guid farmId,
        Guid fieldId,
        CancellationToken cancellationToken)
    {
        var fieldExists = await _db.Fields
            .AnyAsync(
                f => f.Id == fieldId && f.FarmId == farmId,
                cancellationToken);

        if (!fieldExists)
        {
            return NotFound(new { message = "Field not found in this farm." });
        }

        var records = await _db.FieldCrops
            .AsNoTracking()
            .Where(fc =>
                fc.FarmId == farmId &&
                fc.FieldId == fieldId)
            .OrderByDescending(fc => fc.PlantingDate)
            .Select(fc => new FieldCropResponse
            {
                Id = fc.Id,
                FarmId = fc.FarmId,
                FieldId = fc.FieldId,
                CropId = fc.CropId,
                CropName = fc.Crop.Name,
                PlantingDate = fc.PlantingDate,
                ExpectedHarvestDate = fc.ExpectedHarvestDate,
                ActualHarvestDate = fc.ActualHarvestDate,
                AreaHectares = fc.AreaHectares,
                Status = fc.Status,
                CreatedAtUtc = fc.CreatedAtUtc
            })
            .ToListAsync(cancellationToken);

        return Ok(records);
    }

    // GET /api/farms/{farmId}/fields/{fieldId}/crops/{fieldCropId}
    [HttpGet("{fieldCropId:guid}")]
    [FarmPermission(PermissionCodes.CropsView)]
    public async Task<IActionResult> GetById(
        Guid farmId,
        Guid fieldId,
        Guid fieldCropId,
        CancellationToken cancellationToken)
    {
        var record = await _db.FieldCrops
            .AsNoTracking()
            .Where(fc =>
                fc.Id == fieldCropId &&
                fc.FarmId == farmId &&
                fc.FieldId == fieldId)
            .Select(fc => new FieldCropResponse
            {
                Id = fc.Id,
                FarmId = fc.FarmId,
                FieldId = fc.FieldId,
                CropId = fc.CropId,
                CropName = fc.Crop.Name,
                PlantingDate = fc.PlantingDate,
                ExpectedHarvestDate = fc.ExpectedHarvestDate,
                ActualHarvestDate = fc.ActualHarvestDate,
                AreaHectares = fc.AreaHectares,
                Status = fc.Status,
                CreatedAtUtc = fc.CreatedAtUtc
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (record is null)
        {
            return NotFound(new
            {
                message = "Field crop record not found."
            });
        }

        return Ok(record);
    }

    // POST /api/farms/{farmId}/fields/{fieldId}/crops
    [HttpPost]
    [FarmPermission(PermissionCodes.CropsManage)]
    public async Task<IActionResult> Create(
        Guid farmId,
        Guid fieldId,
        [FromBody] CreateFieldCropRequest request,
        CancellationToken cancellationToken)
    {
        if (request.ExpectedHarvestDate.HasValue &&
            request.ExpectedHarvestDate.Value < request.PlantingDate)
        {
            return BadRequest(new
            {
                message = "Expected harvest date cannot be before planting date."
            });
        }

        var field = await _db.Fields
            .AsNoTracking()
            .FirstOrDefaultAsync(
                f => f.Id == fieldId &&
                     f.FarmId == farmId &&
                     f.IsActive &&
                     f.Farm.IsActive,
                cancellationToken);

        if (field is null)
        {
            return NotFound(new
            {
                message = "Active field not found in this farm."
            });
        }

        var crop = await _db.Crops
            .AsNoTracking()
            .FirstOrDefaultAsync(
                c => c.Id == request.CropId && c.IsActive,
                cancellationToken);

        if (crop is null)
        {
            return BadRequest(new
            {
                message = "The selected crop does not exist or is inactive."
            });
        }

        if (request.AreaHectares.HasValue &&
            field.AreaHectares.HasValue &&
            request.AreaHectares.Value > field.AreaHectares.Value)
        {
            return BadRequest(new
            {
                message = "Crop area cannot exceed the field area."
            });
        }

        var record = new FieldCrop
        {
            FarmId = farmId,
            FieldId = fieldId,
            CropId = crop.Id,
            PlantingDate = request.PlantingDate,
            ExpectedHarvestDate = request.ExpectedHarvestDate,
            AreaHectares = request.AreaHectares,
            Status = FieldCropStatus.Planned
        };

        _db.FieldCrops.Add(record);

        await _db.SaveChangesAsync(cancellationToken);

        var response = ToResponse(record, crop.Name);

        return CreatedAtAction(
            nameof(GetById),
            new
            {
                farmId,
                fieldId,
                fieldCropId = record.Id
            },
            response);
    }

    // PUT /api/farms/{farmId}/fields/{fieldId}/crops/{fieldCropId}
    [HttpPut("{fieldCropId:guid}")]
    [FarmPermission(PermissionCodes.CropsManage)]
    public async Task<IActionResult> Update(
        Guid farmId,
        Guid fieldId,
        Guid fieldCropId,
        [FromBody] UpdateFieldCropRequest request,
        CancellationToken cancellationToken)
    {
        if (request.ExpectedHarvestDate.HasValue &&
            request.ExpectedHarvestDate.Value < request.PlantingDate)
        {
            return BadRequest(new
            {
                message = "Expected harvest date cannot be before planting date."
            });
        }

        if (request.ActualHarvestDate.HasValue &&
            request.ActualHarvestDate.Value < request.PlantingDate)
        {
            return BadRequest(new
            {
                message = "Actual harvest date cannot be before planting date."
            });
        }

        var record = await _db.FieldCrops
            .FirstOrDefaultAsync(
                fc =>
                    fc.Id == fieldCropId &&
                    fc.FarmId == farmId &&
                    fc.FieldId == fieldId,
                cancellationToken);

        if (record is null)
        {
            return NotFound(new
            {
                message = "Field crop record not found."
            });
        }

        var crop = await _db.Crops
            .AsNoTracking()
            .FirstOrDefaultAsync(
                c => c.Id == request.CropId && c.IsActive,
                cancellationToken);

        if (crop is null)
        {
            return BadRequest(new
            {
                message = "The selected crop does not exist or is inactive."
            });
        }

        var fieldArea = await _db.Fields
            .Where(f => f.Id == fieldId && f.FarmId == farmId)
            .Select(f => f.AreaHectares)
            .FirstOrDefaultAsync(cancellationToken);

        if (request.AreaHectares.HasValue &&
            fieldArea.HasValue &&
            request.AreaHectares.Value > fieldArea.Value)
        {
            return BadRequest(new
            {
                message = "Crop area cannot exceed the field area."
            });
        }

        record.CropId = crop.Id;
        record.PlantingDate = request.PlantingDate;
        record.ExpectedHarvestDate = request.ExpectedHarvestDate;
        record.ActualHarvestDate = request.ActualHarvestDate;
        record.AreaHectares = request.AreaHectares;

        await _db.SaveChangesAsync(cancellationToken);

        return Ok(ToResponse(record, crop.Name));
    }

    // PATCH /api/farms/{farmId}/fields/{fieldId}/crops/{fieldCropId}/status
    [HttpPatch("{fieldCropId:guid}/status")]
    [FarmPermission(PermissionCodes.CropsManage)]
    public async Task<IActionResult> UpdateStatus(
        Guid farmId,
        Guid fieldId,
        Guid fieldCropId,
        [FromBody] UpdateFieldCropStatusRequest request,
        CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<FieldCropStatus>(
                request.Status,
                ignoreCase: false,
                out var status) ||
            !Enum.IsDefined(status))
        {
            return BadRequest(new
            {
                message = "Invalid crop planting status."
            });
        }

        var record = await _db.FieldCrops
            .FirstOrDefaultAsync(
                fc =>
                    fc.Id == fieldCropId &&
                    fc.FarmId == farmId &&
                    fc.FieldId == fieldId,
                cancellationToken);

        if (record is null)
        {
            return NotFound(new
            {
                message = "Field crop record not found."
            });
        }

        if (status == FieldCropStatus.Harvested &&
            !record.ActualHarvestDate.HasValue)
        {
            return BadRequest(new
            {
                message = "Set the actual harvest date before marking the crop as harvested."
            });
        }

        record.Status = status;

        await _db.SaveChangesAsync(cancellationToken);

        return Ok(new
        {
            message = "Field crop status updated successfully.",
            fieldCropId = record.Id,
            status = record.Status.ToString()
        });
    }

    private static FieldCropResponse ToResponse(
        FieldCrop record,
        string cropName)
    {
        return new FieldCropResponse
        {
            Id = record.Id,
            FarmId = record.FarmId,
            FieldId = record.FieldId,
            CropId = record.CropId,
            CropName = cropName,
            PlantingDate = record.PlantingDate,
            ExpectedHarvestDate = record.ExpectedHarvestDate,
            ActualHarvestDate = record.ActualHarvestDate,
            AreaHectares = record.AreaHectares,
            Status = record.Status,
            CreatedAtUtc = record.CreatedAtUtc
        };
    }
}

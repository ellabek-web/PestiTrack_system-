
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PestiTrackBackend.Data;
using PestiTrackBackend.DTOs.Fields;
using PestiTrackBackend.Models.Farms;
using PestiTrackBackend.Security;
using PestiTrackBackend.Security.Authorization;

namespace PestiTrackBackend.Controllers;

[ApiController]
[Route("api/farms/{farmId:guid}/fields")]
[Authorize]
public sealed class FieldsController : ControllerBase
{
    private readonly AppDbContext _db;

    public FieldsController(AppDbContext db)
    {
        _db = db;
    }

    // GET: api/farms/{farmId}/fields
    [HttpGet]
    [FarmPermission(PermissionCodes.FieldsView)]
    public async Task<ActionResult<List<FieldResponse>>> GetFields(
        Guid farmId,
        CancellationToken cancellationToken)
    {
        var fields = await _db.Fields
            .AsNoTracking()
            .Where(field => field.FarmId == farmId)
            .OrderBy(field => field.Name)
            .Select(field => new FieldResponse
            {
                Id = field.Id,
                FarmId = field.FarmId,
                Name = field.Name,
                Code = field.Code,
                AreaHectares = field.AreaHectares,
                Description = field.Description,
                IsActive = field.IsActive,
                CreatedAtUtc = field.CreatedAtUtc
            })
            .ToListAsync(cancellationToken);

        return Ok(fields);
    }

    // GET: api/farms/{farmId}/fields/{fieldId}
    [HttpGet("{fieldId:guid}")]
    [FarmPermission(PermissionCodes.FieldsView)]
    public async Task<ActionResult<FieldResponse>> GetById(
        Guid farmId,
        Guid fieldId,
        CancellationToken cancellationToken)
    {
        var field = await _db.Fields
            .AsNoTracking()
            .Where(item =>
                item.Id == fieldId &&
                item.FarmId == farmId)
            .Select(item => new FieldResponse
            {
                Id = item.Id,
                FarmId = item.FarmId,
                Name = item.Name,
                Code = item.Code,
                AreaHectares = item.AreaHectares,
                Description = item.Description,
                IsActive = item.IsActive,
                CreatedAtUtc = item.CreatedAtUtc
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (field is null)
            return NotFound(new { message = "Field not found." });

        return Ok(field);
    }

    // POST: api/farms/{farmId}/fields
    [HttpPost]
    [FarmPermission(PermissionCodes.FieldsManage)]
    public async Task<ActionResult<FieldResponse>> Create(
        Guid farmId,
        CreateFieldRequest request,
        CancellationToken cancellationToken)
    {
        var farmIsActive = await _db.Farms
            .AnyAsync(
                farm => farm.Id == farmId && farm.IsActive,
                cancellationToken);

        if (!farmIsActive)
            return NotFound(new { message = "Active farm not found." });

        var code = request.Code.Trim().ToUpperInvariant();

        var codeExists = await _db.Fields
            .AnyAsync(
                field => field.FarmId == farmId && field.Code == code,
                cancellationToken);

        if (codeExists)
        {
            return Conflict(new
            {
                message = "A field with this code already exists in this farm."
            });
        }

        var field = new Field
        {
            FarmId = farmId,
            Name = request.Name.Trim(),
            Code = code,
            AreaHectares = request.AreaHectares,
            Description = string.IsNullOrWhiteSpace(request.Description)
                ? null
                : request.Description.Trim(),
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        _db.Fields.Add(field);
        await _db.SaveChangesAsync(cancellationToken);

        var response = ToResponse(field);

        return CreatedAtAction(
            nameof(GetById),
            new { farmId, fieldId = field.Id },
            response);
    }

    // PUT: api/farms/{farmId}/fields/{fieldId}
    [HttpPut("{fieldId:guid}")]
    [FarmPermission(PermissionCodes.FieldsManage)]
    public async Task<ActionResult<FieldResponse>> Update(
        Guid farmId,
        Guid fieldId,
        UpdateFieldRequest request,
        CancellationToken cancellationToken)
    {
        var field = await _db.Fields
            .FirstOrDefaultAsync(
                item =>
                    item.Id == fieldId &&
                    item.FarmId == farmId,
                cancellationToken);

        if (field is null)
            return NotFound(new { message = "Field not found." });

        var code = request.Code.Trim().ToUpperInvariant();

        var codeExists = await _db.Fields
            .AnyAsync(
                item =>
                    item.FarmId == farmId &&
                    item.Code == code &&
                    item.Id != fieldId,
                cancellationToken);

        if (codeExists)
        {
            return Conflict(new
            {
                message = "Another field with this code already exists in this farm."
            });
        }

        field.Name = request.Name.Trim();
        field.Code = code;
        field.AreaHectares = request.AreaHectares;
        field.Description = string.IsNullOrWhiteSpace(request.Description)
            ? null
            : request.Description.Trim();

        await _db.SaveChangesAsync(cancellationToken);

        return Ok(ToResponse(field));
    }

    // PATCH: api/farms/{farmId}/fields/{fieldId}/status
    [HttpPatch("{fieldId:guid}/status")]
    [FarmPermission(PermissionCodes.FieldsManage)]
    public async Task<IActionResult> UpdateStatus(
        Guid farmId,
        Guid fieldId,
        UpdateFieldStatusRequest request,
        CancellationToken cancellationToken)
    {
        var field = await _db.Fields
            .FirstOrDefaultAsync(
                item =>
                    item.Id == fieldId &&
                    item.FarmId == farmId,
                cancellationToken);

        if (field is null)
            return NotFound(new { message = "Field not found." });

        field.IsActive = request.IsActive;

        await _db.SaveChangesAsync(cancellationToken);

        return Ok(new
        {
            message = request.IsActive
                ? "Field activated successfully."
                : "Field deactivated successfully.",
            fieldId = field.Id,
            isActive = field.IsActive
        });
    }

    private static FieldResponse ToResponse(Field field)
    {
        return new FieldResponse
        {
            Id = field.Id,
            FarmId = field.FarmId,
            Name = field.Name,
            Code = field.Code,
            AreaHectares = field.AreaHectares,
            Description = field.Description,
            IsActive = field.IsActive,
            CreatedAtUtc = field.CreatedAtUtc
        };
    }
}

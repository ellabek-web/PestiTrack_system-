using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PestiTrackBackend.Data;
using PestiTrackBackend.DTOs.Plots;
using PestiTrackBackend.Models.Farms;
using PestiTrackBackend.Security;
using PestiTrackBackend.Security.Authorization;

namespace PestiTrackBackend.Controllers;

[ApiController]
[Route("api/farms/{farmId:guid}/plots")]
[Authorize]
public sealed class PlotsController : ControllerBase
{
    private readonly AppDbContext _db;

    public PlotsController(AppDbContext db)
    {
        _db = db;
    }

    // GET: api/farms/{farmId}/plots?fieldId={fieldId}
    [HttpGet]
    [FarmPermission(PermissionCodes.PlotsView)]
    public async Task<ActionResult<List<PlotResponse>>> GetPlots(
        Guid farmId,
        [FromQuery] Guid? fieldId,
        CancellationToken cancellationToken)
    {
        var plots = await _db.Plots
            .AsNoTracking()
            .Include(plot => plot.Field)
            .Include(plot => plot.Crop)
            .Where(plot =>
                plot.FarmId == farmId &&
                (!fieldId.HasValue || plot.FieldId == fieldId.Value))
            .OrderBy(plot => plot.Field.Name)
            .ThenBy(plot => plot.PlotNumber)
            .ToListAsync(cancellationToken);

        return Ok(plots.Select(ToResponse).ToList());
    }

    // GET: api/farms/{farmId}/plots/{plotId}
    [HttpGet("{plotId:guid}")]
    [FarmPermission(PermissionCodes.PlotsView)]
    public async Task<ActionResult<PlotResponse>> GetById(
        Guid farmId,
        Guid plotId,
        CancellationToken cancellationToken)
    {
        var plot = await _db.Plots
            .AsNoTracking()
            .Include(item => item.Field)
            .Include(item => item.Crop)
            .FirstOrDefaultAsync(
                item => item.Id == plotId && item.FarmId == farmId,
                cancellationToken);

        if (plot is null)
            return NotFound(new { message = "Plot not found." });

        return Ok(ToResponse(plot));
    }

    // POST: api/farms/{farmId}/plots
    [HttpPost]
    [FarmPermission(PermissionCodes.PlotsManage)]
    public async Task<ActionResult<PlotResponse>> Create(
        Guid farmId,
        CreatePlotRequest request,
        CancellationToken cancellationToken)
    {
        var farmIsActive = await _db.Farms
            .AnyAsync(
                farm => farm.Id == farmId && farm.IsActive,
                cancellationToken);

        if (!farmIsActive)
            return NotFound(new { message = "Active farm not found." });

        var fieldError = await ValidateFieldAndCropAsync(
            farmId,
            request.FieldId,
            request.CropId,
            cancellationToken);

        if (fieldError is not null)
            return BadRequest(new { message = fieldError });

        if (!PlotAreaUnits.All.Contains(request.AreaUnit, StringComparer.Ordinal))
        {
            return BadRequest(new
            {
                message = "Area unit must be Hectares, SquareMeters, or Acres."
            });
        }

        var plotNumber = request.PlotNumber.Trim();

        var plotNumberExists = await _db.Plots
            .AnyAsync(
                plot =>
                    plot.FarmId == farmId &&
                    plot.FieldId == request.FieldId &&
                    plot.PlotNumber == plotNumber,
                cancellationToken);

        if (plotNumberExists)
        {
            return Conflict(new
            {
                message = "A plot with this number already exists in this field."
            });
        }

        var plot = new Plot
        {
            FarmId = farmId,
            FieldId = request.FieldId,
            PlotNumber = plotNumber,
            Name = request.Name.Trim(),
            AreaValue = request.AreaValue,
            AreaUnit = request.AreaUnit,
            Location = request.Location.Trim(),
            CropId = request.CropId,
            Comment = string.IsNullOrWhiteSpace(request.Comment)
                ? null
                : request.Comment.Trim(),
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        _db.Plots.Add(plot);
        await _db.SaveChangesAsync(cancellationToken);

        var created = await _db.Plots
            .AsNoTracking()
            .Include(item => item.Field)
            .Include(item => item.Crop)
            .FirstAsync(item => item.Id == plot.Id, cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { farmId, plotId = plot.Id },
            ToResponse(created));
    }

    // PUT: api/farms/{farmId}/plots/{plotId}
    [HttpPut("{plotId:guid}")]
    [FarmPermission(PermissionCodes.PlotsManage)]
    public async Task<ActionResult<PlotResponse>> Update(
        Guid farmId,
        Guid plotId,
        UpdatePlotRequest request,
        CancellationToken cancellationToken)
    {
        var plot = await _db.Plots
            .Include(item => item.Field)
            .Include(item => item.Crop)
            .FirstOrDefaultAsync(
                item => item.Id == plotId && item.FarmId == farmId,
                cancellationToken);

        if (plot is null)
            return NotFound(new { message = "Plot not found." });

        var fieldError = await ValidateFieldAndCropAsync(
            farmId,
            request.FieldId,
            request.CropId,
            cancellationToken);

        if (fieldError is not null)
            return BadRequest(new { message = fieldError });

        if (!PlotAreaUnits.All.Contains(request.AreaUnit, StringComparer.Ordinal))
        {
            return BadRequest(new
            {
                message = "Area unit must be Hectares, SquareMeters, or Acres."
            });
        }

        var plotNumber = request.PlotNumber.Trim();

        var plotNumberExists = await _db.Plots
            .AnyAsync(
                item =>
                    item.FarmId == farmId &&
                    item.FieldId == request.FieldId &&
                    item.PlotNumber == plotNumber &&
                    item.Id != plotId,
                cancellationToken);

        if (plotNumberExists)
        {
            return Conflict(new
            {
                message = "Another plot with this number already exists in this field."
            });
        }

        plot.FieldId = request.FieldId;
        plot.PlotNumber = plotNumber;
        plot.Name = request.Name.Trim();
        plot.AreaValue = request.AreaValue;
        plot.AreaUnit = request.AreaUnit;
        plot.Location = request.Location.Trim();
        plot.CropId = request.CropId;
        plot.Comment = string.IsNullOrWhiteSpace(request.Comment)
            ? null
            : request.Comment.Trim();

        await _db.SaveChangesAsync(cancellationToken);

        // Reload the field/crop names in case the plot moved.
        await _db.Entry(plot).Reference(item => item.Field).LoadAsync(cancellationToken);
        await _db.Entry(plot).Reference(item => item.Crop).LoadAsync(cancellationToken);

        return Ok(ToResponse(plot));
    }

    // PATCH: api/farms/{farmId}/plots/{plotId}/status
    [HttpPatch("{plotId:guid}/status")]
    [FarmPermission(PermissionCodes.PlotsManage)]
    public async Task<IActionResult> UpdateStatus(
        Guid farmId,
        Guid plotId,
        UpdatePlotStatusRequest request,
        CancellationToken cancellationToken)
    {
        var plot = await _db.Plots
            .FirstOrDefaultAsync(
                item => item.Id == plotId && item.FarmId == farmId,
                cancellationToken);

        if (plot is null)
            return NotFound(new { message = "Plot not found." });

        plot.IsActive = request.IsActive;

        await _db.SaveChangesAsync(cancellationToken);

        return Ok(new
        {
            message = request.IsActive
                ? "Plot activated successfully."
                : "Plot deactivated successfully.",
            plotId = plot.Id,
            isActive = plot.IsActive
        });
    }

    private async Task<string?> ValidateFieldAndCropAsync(
        Guid farmId,
        Guid fieldId,
        Guid cropId,
        CancellationToken cancellationToken)
    {
        var fieldIsActive = await _db.Fields
            .AnyAsync(
                field =>
                    field.Id == fieldId &&
                    field.FarmId == farmId &&
                    field.IsActive,
                cancellationToken);

        if (!fieldIsActive)
            return "The selected field does not exist or is inactive in this farm.";

        var cropIsActive = await _db.Crops
            .AnyAsync(
                crop => crop.Id == cropId && crop.IsActive,
                cancellationToken);

        if (!cropIsActive)
            return "The selected crop does not exist or is inactive.";

        return null;
    }

    private static PlotResponse ToResponse(Plot plot)
    {
        return new PlotResponse
        {
            Id = plot.Id,
            FarmId = plot.FarmId,
            FieldId = plot.FieldId,
            FieldName = plot.Field.Name,
            PlotNumber = plot.PlotNumber,
            Name = plot.Name,
            AreaValue = plot.AreaValue,
            AreaUnit = plot.AreaUnit,
            AreaHectares = PlotAreaUnits.ToHectares(plot.AreaValue, plot.AreaUnit),
            Location = plot.Location,
            CropId = plot.CropId,
            CropName = plot.Crop.Name,
            Comment = plot.Comment,
            IsActive = plot.IsActive,
            CreatedAtUtc = plot.CreatedAtUtc
        };
    }
}

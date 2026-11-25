using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PestiTrackBackend.Data;
using PestiTrackBackend.DTOs.Compliance;
using PestiTrackBackend.Models.Applications;
using PestiTrackBackend.Models.Compliance;
using PestiTrackBackend.Models.Crops;
using PestiTrackBackend.Security;
using PestiTrackBackend.Security.Authorization;

namespace PestiTrackBackend.Controllers;

[ApiController]
[Route("api/farms/{farmId:guid}/compliance")]
[Authorize]
public sealed class ComplianceController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly TimeProvider _timeProvider;

    public ComplianceController(
        AppDbContext db,
        TimeProvider timeProvider)
    {
        _db = db;
        _timeProvider = timeProvider;
    }

    // GET: api/farms/{farmId}/compliance/overview
    [HttpGet("overview")]
    [FarmPermission(PermissionCodes.ComplianceView)]
    public async Task<ActionResult<ComplianceOverviewResponse>> GetOverview(
        Guid farmId,
        CancellationToken cancellationToken)
    {
        var farmExists = await _db.Farms
            .AsNoTracking()
            .AnyAsync(f => f.Id == farmId && f.IsActive, cancellationToken);

        if (!farmExists)
            return NotFound(new { message = "Active farm not found." });

        var nowOffset = _timeProvider.GetUtcNow();
        var nowUtc = nowOffset.UtcDateTime;
        var today = DateOnly.FromDateTime(nowUtc);

        // 1. Compliance record counts.
        var recordStatuses = await _db.ComplianceRecords
            .AsNoTracking()
            .Where(r => r.FarmId == farmId)
            .GroupBy(r => r.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var openRecords = recordStatuses
            .Where(s =>
                s.Status == ComplianceRecordStatus.Open ||
                s.Status == ComplianceRecordStatus.InReview)
            .Sum(s => s.Count);
        var nonCompliantRecords = recordStatuses
            .Where(s => s.Status == ComplianceRecordStatus.NonCompliant)
            .Sum(s => s.Count);

        // 2. Worker certification monitoring (active applicators).
        var applicators = await _db.Workers
            .AsNoTracking()
            .Where(w => w.FarmId == farmId && w.IsActive && w.IsApplicator)
            .OrderBy(w => w.CertificationExpiryDate)
            .ThenBy(w => w.FullName)
            .ToListAsync(cancellationToken);

        var certifications = new List<WorkerCertificationStatusItem>();
        foreach (var worker in applicators)
        {
            DateOnly? expiry = worker.CertificationExpiryDate.HasValue
                ? DateOnly.FromDateTime(worker.CertificationExpiryDate.Value)
                : null;
            int? daysRemaining = expiry.HasValue
                ? expiry.Value.DayNumber - today.DayNumber
                : null;

            string status;
            if (string.IsNullOrWhiteSpace(worker.CertificationNumber) || !expiry.HasValue)
                status = "Missing";
            else if (daysRemaining < 0)
                status = "Expired";
            else if (daysRemaining <= 30)
                status = "ExpiringSoon";
            else
                status = "Valid";

            certifications.Add(new WorkerCertificationStatusItem
            {
                WorkerId = worker.Id,
                FullName = worker.FullName,
                WorkerCode = worker.WorkerCode,
                CertificationNumber = worker.CertificationNumber,
                CertificationExpiryDate = expiry,
                Status = status,
                DaysRemaining = daysRemaining
            });
        }

        // 3. Active REI and PHI holds from completed applications.
        var completedApps = await _db.PesticideApplications
            .AsNoTracking()
            .Where(a =>
                a.FarmId == farmId &&
                a.Status == PesticideApplicationStatus.Completed &&
                a.Products.Any(p =>
                    p.Product.ReEntryIntervalHours.HasValue ||
                    p.Product.PreHarvestIntervalDays.HasValue))
            .Select(a => new
            {
                a.Id,
                a.FieldId,
                FieldName = a.Field.Name,
                CropName = a.FieldCrop.Crop.Name,
                CropStatus = a.FieldCrop.Status,
                ExpectedHarvestDate = a.FieldCrop.ExpectedHarvestDate,
                a.ApplicationDateUtc,
                Products = a.Products.Select(p => new
                {
                    p.Product.Name,
                    p.Product.ReEntryIntervalHours,
                    p.Product.PreHarvestIntervalDays
                }).ToList()
            })
            .ToListAsync(cancellationToken);

        var reEntryHolds = new List<ReEntryHoldItem>();
        var preHarvestHolds = new List<PreHarvestHoldItem>();

        foreach (var app in completedApps)
        {
            var reiHours = app.Products
                .Where(p => p.ReEntryIntervalHours.HasValue && p.ReEntryIntervalHours.Value > 0)
                .Select(p => p.ReEntryIntervalHours!.Value)
                .DefaultIfEmpty(0)
                .Max();

            if (reiHours > 0)
            {
                var allowedAt = DateTime.SpecifyKind(
                    app.ApplicationDateUtc.AddHours(reiHours),
                    DateTimeKind.Utc);
                if (allowedAt > nowUtc)
                {
                    reEntryHolds.Add(new ReEntryHoldItem
                    {
                        ApplicationId = app.Id,
                        FieldId = app.FieldId,
                        FieldName = app.FieldName,
                        CropName = app.CropName,
                        ApplicationDateUtc = ToUtcOffset(app.ApplicationDateUtc),
                        ReEntryIntervalHours = reiHours,
                        ReEntryAllowedAtUtc = new DateTimeOffset(allowedAt),
                        Products = app.Products
                            .Where(p => p.ReEntryIntervalHours > 0)
                            .Select(p => p.Name)
                            .Distinct()
                            .ToList()
                    });
                }
            }

            if (app.CropStatus == FieldCropStatus.Planted)
            {
                var phiDays = app.Products
                    .Where(p => p.PreHarvestIntervalDays.HasValue && p.PreHarvestIntervalDays.Value > 0)
                    .Select(p => p.PreHarvestIntervalDays!.Value)
                    .DefaultIfEmpty(0)
                    .Max();

                if (phiDays > 0)
                {
                    var appDate = DateOnly.FromDateTime(app.ApplicationDateUtc);
                    var earliestHarvest = appDate.AddDays(phiDays);
                    if (earliestHarvest > today)
                    {
                        preHarvestHolds.Add(new PreHarvestHoldItem
                        {
                            ApplicationId = app.Id,
                            FieldId = app.FieldId,
                            FieldName = app.FieldName,
                            CropName = app.CropName,
                            ApplicationDate = appDate,
                            PreHarvestIntervalDays = phiDays,
                            EarliestSafeHarvestDate = earliestHarvest,
                            ExpectedHarvestDate = app.ExpectedHarvestDate,
                            ConflictsWithExpectedHarvest =
                                app.ExpectedHarvestDate.HasValue &&
                                app.ExpectedHarvestDate.Value < earliestHarvest,
                            Products = app.Products
                                .Where(p => p.PreHarvestIntervalDays > 0)
                                .Select(p => p.Name)
                                .Distinct()
                                .ToList()
                        });
                    }
                }
            }
        }

        // 4. Expiring / expired active product registrations (within 60 days).
        var regCutoff = today.AddDays(60);
        var expiringProducts = await _db.Products
            .AsNoTracking()
            .Where(p =>
                p.IsActive &&
                p.RegistrationExpiryDate.HasValue &&
                p.RegistrationExpiryDate.Value <= regCutoff)
            .OrderBy(p => p.RegistrationExpiryDate)
            .ThenBy(p => p.Name)
            .ToListAsync(cancellationToken);

        var productRegistrations = expiringProducts
            .Select(p =>
            {
                var expiry = p.RegistrationExpiryDate!.Value;
                var days = expiry.DayNumber - today.DayNumber;
                return new ProductRegistrationAlertItem
                {
                    ProductId = p.Id,
                    ProductName = p.Name,
                    RegistrationNumber = p.RegistrationNumber,
                    RegistrationExpiryDate = expiry,
                    Status = days < 0 ? "Expired" : "ExpiringSoon",
                    DaysRemaining = days
                };
            })
            .ToList();

        return Ok(new ComplianceOverviewResponse
        {
            GeneratedAtUtc = nowOffset,
            OpenRecords = openRecords,
            NonCompliantRecords = nonCompliantRecords,
            ExpiredCertifications = certifications.Count(c =>
                c.Status is "Expired" or "Missing"),
            ExpiringCertificationsWithin30Days = certifications.Count(c =>
                c.Status == "ExpiringSoon"),
            ActiveReEntryHolds = reEntryHolds.Count,
            ActivePreHarvestHolds = preHarvestHolds.Count,
            ExpiringProductRegistrations = productRegistrations.Count,
            Certifications = certifications,
            ReEntryHolds = reEntryHolds
                .OrderBy(h => h.ReEntryAllowedAtUtc)
                .ToList(),
            PreHarvestHolds = preHarvestHolds
                .OrderBy(h => h.EarliestSafeHarvestDate)
                .ToList(),
            ProductRegistrations = productRegistrations
        });
    }

    // GET: api/farms/{farmId}/compliance/records
    [HttpGet("records")]
    [FarmPermission(PermissionCodes.ComplianceView)]
    public async Task<ActionResult<ComplianceRecordListResponse>> GetRecords(
        Guid farmId,
        [FromQuery] ComplianceRecordQueryRequest request,
        CancellationToken cancellationToken)
    {
        var query = _db.ComplianceRecords
            .AsNoTracking()
            .Where(r => r.FarmId == farmId);

        if (request.Status.HasValue)
            query = query.Where(r => r.Status == request.Status.Value);

        if (request.RecordType.HasValue)
            query = query.Where(r => r.RecordType == request.RecordType.Value);

        if (request.Severity.HasValue)
            query = query.Where(r => r.Severity == request.Severity.Value);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();
            query = query.Where(r =>
                r.Title.Contains(search) ||
                (r.InspectorName != null && r.InspectorName.Contains(search)) ||
                (r.Findings != null && r.Findings.Contains(search)) ||
                (r.CorrectiveAction != null && r.CorrectiveAction.Contains(search)));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var rows = await query
            .OrderByDescending(r => r.CheckedAtUtc)
            .ThenByDescending(r => r.CreatedAtUtc)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(r => new
            {
                r.Id,
                r.FarmId,
                r.FieldId,
                FieldName = r.Field != null ? r.Field.Name : null,
                r.PesticideApplicationId,
                r.WorkerId,
                WorkerName = r.Worker != null ? r.Worker.FullName : null,
                r.RecordType,
                r.Title,
                r.Status,
                r.Severity,
                r.CheckedAtUtc,
                r.DueDateUtc,
                r.ResolvedAtUtc,
                r.InspectorName,
                r.Findings,
                r.CorrectiveAction,
                r.CreatedByUserId,
                CreatedByDisplayName = r.CreatedByUser.DisplayName,
                r.CreatedAtUtc
            })
            .ToListAsync(cancellationToken);

        var items = rows.Select(r => new ComplianceRecordResponse
        {
            Id = r.Id,
            FarmId = r.FarmId,
            FieldId = r.FieldId,
            FieldName = r.FieldName,
            PesticideApplicationId = r.PesticideApplicationId,
            WorkerId = r.WorkerId,
            WorkerName = r.WorkerName,
            RecordType = r.RecordType.ToString(),
            Title = r.Title,
            Status = r.Status.ToString(),
            Severity = r.Severity.ToString(),
            CheckedAtUtc = ToUtcOffset(r.CheckedAtUtc),
            DueDateUtc = r.DueDateUtc.HasValue ? ToUtcOffset(r.DueDateUtc.Value) : null,
            ResolvedAtUtc = r.ResolvedAtUtc.HasValue ? ToUtcOffset(r.ResolvedAtUtc.Value) : null,
            InspectorName = r.InspectorName,
            Findings = r.Findings,
            CorrectiveAction = r.CorrectiveAction,
            CreatedByUserId = r.CreatedByUserId,
            CreatedByDisplayName = r.CreatedByDisplayName,
            CreatedAtUtc = ToUtcOffset(r.CreatedAtUtc)
        }).ToList();

        return Ok(new ComplianceRecordListResponse
        {
            Page = request.Page,
            PageSize = request.PageSize,
            TotalCount = totalCount,
            TotalPages = totalCount == 0
                ? 0
                : (int)Math.Ceiling(totalCount / (double)request.PageSize),
            Items = items
        });
    }

    // POST: api/farms/{farmId}/compliance/records
    [HttpPost("records")]
    [FarmPermission(PermissionCodes.ComplianceManage)]
    public async Task<ActionResult<ComplianceRecordResponse>> CreateRecord(
        Guid farmId,
        CreateComplianceRecordRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
            return Unauthorized();

        var validationError = await ValidateReferencesAsync(
            farmId,
            request.FieldId,
            request.WorkerId,
            request.PesticideApplicationId,
            cancellationToken);

        if (validationError is not null)
            return BadRequest(new { message = validationError });

        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
        var checkedAtUtc = request.CheckedAtUtc == default
            ? nowUtc
            : DateTime.SpecifyKind(request.CheckedAtUtc, DateTimeKind.Utc);

        var record = new ComplianceRecord
        {
            Id = Guid.NewGuid(),
            FarmId = farmId,
            FieldId = request.FieldId,
            PesticideApplicationId = request.PesticideApplicationId,
            WorkerId = request.WorkerId,
            RecordType = request.RecordType,
            Title = request.Title.Trim(),
            Status = request.Status,
            Severity = request.Severity,
            CheckedAtUtc = checkedAtUtc,
            DueDateUtc = request.DueDateUtc.HasValue
                ? DateTime.SpecifyKind(request.DueDateUtc.Value, DateTimeKind.Utc)
                : null,
            ResolvedAtUtc = request.Status == ComplianceRecordStatus.Resolved
                ? nowUtc
                : null,
            InspectorName = Normalize(request.InspectorName),
            Findings = Normalize(request.Findings),
            CorrectiveAction = Normalize(request.CorrectiveAction),
            CreatedByUserId = userId.Value,
            CreatedAtUtc = nowUtc
        };

        _db.ComplianceRecords.Add(record);
        await _db.SaveChangesAsync(cancellationToken);

        var response = await LoadRecordResponseAsync(farmId, record.Id, cancellationToken);
        return Created(
            $"/api/farms/{farmId}/compliance/records/{record.Id}",
            response);
    }

    // PUT: api/farms/{farmId}/compliance/records/{recordId}
    [HttpPut("records/{recordId:guid}")]
    [FarmPermission(PermissionCodes.ComplianceManage)]
    public async Task<ActionResult<ComplianceRecordResponse>> UpdateRecord(
        Guid farmId,
        Guid recordId,
        UpdateComplianceRecordRequest request,
        CancellationToken cancellationToken)
    {
        var record = await _db.ComplianceRecords
            .FirstOrDefaultAsync(
                r => r.Id == recordId && r.FarmId == farmId,
                cancellationToken);

        if (record is null)
            return NotFound(new { message = "Compliance record not found." });

        var validationError = await ValidateReferencesAsync(
            farmId,
            request.FieldId,
            request.WorkerId,
            request.PesticideApplicationId,
            cancellationToken);

        if (validationError is not null)
            return BadRequest(new { message = validationError });

        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;

        record.FieldId = request.FieldId;
        record.PesticideApplicationId = request.PesticideApplicationId;
        record.WorkerId = request.WorkerId;
        record.RecordType = request.RecordType;
        record.Title = request.Title.Trim();
        record.Severity = request.Severity;
        record.CheckedAtUtc = request.CheckedAtUtc == default
            ? record.CheckedAtUtc
            : DateTime.SpecifyKind(request.CheckedAtUtc, DateTimeKind.Utc);
        record.DueDateUtc = request.DueDateUtc.HasValue
            ? DateTime.SpecifyKind(request.DueDateUtc.Value, DateTimeKind.Utc)
            : null;
        record.InspectorName = Normalize(request.InspectorName);
        record.Findings = Normalize(request.Findings);
        record.CorrectiveAction = Normalize(request.CorrectiveAction);

        if (request.Status == ComplianceRecordStatus.Resolved &&
            record.Status != ComplianceRecordStatus.Resolved)
        {
            record.ResolvedAtUtc = nowUtc;
        }
        else if (request.Status != ComplianceRecordStatus.Resolved)
        {
            record.ResolvedAtUtc = null;
        }

        record.Status = request.Status;

        await _db.SaveChangesAsync(cancellationToken);

        var response = await LoadRecordResponseAsync(farmId, record.Id, cancellationToken);
        return Ok(response);
    }

    // PATCH: api/farms/{farmId}/compliance/records/{recordId}/status
    [HttpPatch("records/{recordId:guid}/status")]
    [FarmPermission(PermissionCodes.ComplianceManage)]
    public async Task<ActionResult<ComplianceRecordResponse>> UpdateStatus(
        Guid farmId,
        Guid recordId,
        UpdateComplianceRecordStatusRequest request,
        CancellationToken cancellationToken)
    {
        var record = await _db.ComplianceRecords
            .FirstOrDefaultAsync(
                r => r.Id == recordId && r.FarmId == farmId,
                cancellationToken);

        if (record is null)
            return NotFound(new { message = "Compliance record not found." });

        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;

        if (request.Status == ComplianceRecordStatus.Resolved &&
            record.Status != ComplianceRecordStatus.Resolved)
        {
            record.ResolvedAtUtc = nowUtc;
        }
        else if (request.Status != ComplianceRecordStatus.Resolved)
        {
            record.ResolvedAtUtc = null;
        }

        record.Status = request.Status;

        if (request.CorrectiveAction is not null)
            record.CorrectiveAction = Normalize(request.CorrectiveAction);

        await _db.SaveChangesAsync(cancellationToken);

        var response = await LoadRecordResponseAsync(farmId, record.Id, cancellationToken);
        return Ok(response);
    }

    private async Task<string?> ValidateReferencesAsync(
        Guid farmId,
        Guid? fieldId,
        Guid? workerId,
        Guid? applicationId,
        CancellationToken cancellationToken)
    {
        var farmExists = await _db.Farms
            .AsNoTracking()
            .AnyAsync(f => f.Id == farmId && f.IsActive, cancellationToken);

        if (!farmExists)
            return "Active farm not found.";

        if (fieldId.HasValue)
        {
            var fieldExists = await _db.Fields
                .AsNoTracking()
                .AnyAsync(f => f.Id == fieldId.Value && f.FarmId == farmId, cancellationToken);
            if (!fieldExists)
                return "The selected field does not belong to this farm.";
        }

        if (workerId.HasValue)
        {
            var workerExists = await _db.Workers
                .AsNoTracking()
                .AnyAsync(w => w.Id == workerId.Value && w.FarmId == farmId, cancellationToken);
            if (!workerExists)
                return "The selected worker does not belong to this farm.";
        }

        if (applicationId.HasValue)
        {
            var appExists = await _db.PesticideApplications
                .AsNoTracking()
                .AnyAsync(a => a.Id == applicationId.Value && a.FarmId == farmId, cancellationToken);
            if (!appExists)
                return "The selected application does not belong to this farm.";
        }

        return null;
    }

    private async Task<ComplianceRecordResponse> LoadRecordResponseAsync(
        Guid farmId,
        Guid recordId,
        CancellationToken cancellationToken)
    {
        var row = await _db.ComplianceRecords
            .AsNoTracking()
            .Where(r => r.Id == recordId && r.FarmId == farmId)
            .Select(r => new
            {
                r.Id,
                r.FarmId,
                r.FieldId,
                FieldName = r.Field != null ? r.Field.Name : null,
                r.PesticideApplicationId,
                r.WorkerId,
                WorkerName = r.Worker != null ? r.Worker.FullName : null,
                r.RecordType,
                r.Title,
                r.Status,
                r.Severity,
                r.CheckedAtUtc,
                r.DueDateUtc,
                r.ResolvedAtUtc,
                r.InspectorName,
                r.Findings,
                r.CorrectiveAction,
                r.CreatedByUserId,
                CreatedByDisplayName = r.CreatedByUser.DisplayName,
                r.CreatedAtUtc
            })
            .SingleAsync(cancellationToken);

        return new ComplianceRecordResponse
        {
            Id = row.Id,
            FarmId = row.FarmId,
            FieldId = row.FieldId,
            FieldName = row.FieldName,
            PesticideApplicationId = row.PesticideApplicationId,
            WorkerId = row.WorkerId,
            WorkerName = row.WorkerName,
            RecordType = row.RecordType.ToString(),
            Title = row.Title,
            Status = row.Status.ToString(),
            Severity = row.Severity.ToString(),
            CheckedAtUtc = ToUtcOffset(row.CheckedAtUtc),
            DueDateUtc = row.DueDateUtc.HasValue ? ToUtcOffset(row.DueDateUtc.Value) : null,
            ResolvedAtUtc = row.ResolvedAtUtc.HasValue ? ToUtcOffset(row.ResolvedAtUtc.Value) : null,
            InspectorName = row.InspectorName,
            Findings = row.Findings,
            CorrectiveAction = row.CorrectiveAction,
            CreatedByUserId = row.CreatedByUserId,
            CreatedByDisplayName = row.CreatedByDisplayName,
            CreatedAtUtc = ToUtcOffset(row.CreatedAtUtc)
        };
    }

    private Guid? GetCurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var userId) ? userId : null;
    }

    private static DateTimeOffset ToUtcOffset(DateTime value)
    {
        return new DateTimeOffset(
            DateTime.SpecifyKind(value, DateTimeKind.Utc));
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}

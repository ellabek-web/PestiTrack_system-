
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PestiTrackBackend.Data;
using PestiTrackBackend.DTOs.Workers;
using PestiTrackBackend.Models.Workers;
using PestiTrackBackend.Security.Authorization;

namespace PestiTrackBackend.Controllers;

[ApiController]
[Route("api/farms/{farmId:guid}/workers")]
[Authorize]
public sealed class WorkersController : ControllerBase
{
    private readonly AppDbContext _db;

    public WorkersController(AppDbContext db)
    {
        _db = db;
    }

    // GET: api/farms/{farmId}/workers
    [HttpGet]
    [FarmPermission("workers.view")]
    public async Task<ActionResult<IEnumerable<WorkerResponse>>> GetAll(
        Guid farmId,
        CancellationToken cancellationToken)
    {
        var farmExists = await _db.Farms
            .AsNoTracking()
            .AnyAsync(f => f.Id == farmId && f.IsActive, cancellationToken);

        if (!farmExists)
            return NotFound(new { message = "Farm not found or inactive." });

        var workers = await _db.Workers
            .AsNoTracking()
            .Where(w => w.FarmId == farmId)
            .OrderBy(w => w.FullName)
            .Select(w => new WorkerResponse
            {
                Id = w.Id,
                FarmId = w.FarmId,
                FullName = w.FullName,
                WorkerCode = w.WorkerCode,
                PhoneNumber = w.PhoneNumber,
                Email = w.Email,
                IsApplicator = w.IsApplicator,
                CertificationNumber = w.CertificationNumber,
                CertificationExpiryDate = w.CertificationExpiryDate,
                IsActive = w.IsActive,
                CreatedAtUtc = w.CreatedAtUtc
            })
            .ToListAsync(cancellationToken);

        return Ok(workers);
    }

    // GET: api/farms/{farmId}/workers/{workerId}
    [HttpGet("{workerId:guid}")]
    [FarmPermission("workers.view")]
    public async Task<ActionResult<WorkerResponse>> GetById(
        Guid farmId,
        Guid workerId,
        CancellationToken cancellationToken)
    {
        var farmExists = await _db.Farms
            .AsNoTracking()
            .AnyAsync(f => f.Id == farmId && f.IsActive, cancellationToken);

        if (!farmExists)
            return NotFound(new { message = "Farm not found or inactive." });

        var worker = await _db.Workers
            .AsNoTracking()
            .Where(w => w.Id == workerId && w.FarmId == farmId)
            .Select(w => new WorkerResponse
            {
                Id = w.Id,
                FarmId = w.FarmId,
                FullName = w.FullName,
                WorkerCode = w.WorkerCode,
                PhoneNumber = w.PhoneNumber,
                Email = w.Email,
                IsApplicator = w.IsApplicator,
                CertificationNumber = w.CertificationNumber,
                CertificationExpiryDate = w.CertificationExpiryDate,
                IsActive = w.IsActive,
                CreatedAtUtc = w.CreatedAtUtc
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (worker is null)
            return NotFound(new { message = "Worker not found." });

        return Ok(worker);
    }

    // POST: api/farms/{farmId}/workers
    [HttpPost]
    [FarmPermission("workers.manage")]
    public async Task<ActionResult<WorkerResponse>> Create(
        Guid farmId,
        CreateWorkerRequest request,
        CancellationToken cancellationToken)
    {
        var farmExists = await _db.Farms
            .AnyAsync(f => f.Id == farmId && f.IsActive, cancellationToken);

        if (!farmExists)
            return NotFound(new { message = "Farm not found or inactive." });

        var workerCode = request.WorkerCode.Trim().ToUpperInvariant();

        var duplicateCode = await _db.Workers
            .AnyAsync(
                w => w.FarmId == farmId && w.WorkerCode == workerCode,
                cancellationToken);

        if (duplicateCode)
            return Conflict(new { message = "A worker with this code already exists in this farm." });

        var worker = new Worker
        {
            FarmId = farmId,
            FullName = request.FullName.Trim(),
            WorkerCode = workerCode,
            PhoneNumber = Normalize(request.PhoneNumber),
            Email = Normalize(request.Email)?.ToLowerInvariant(),
            IsApplicator = request.IsApplicator,
            CertificationNumber = Normalize(request.CertificationNumber),
            CertificationExpiryDate = request.CertificationExpiryDate.HasValue
                ? DateTime.SpecifyKind(
                    request.CertificationExpiryDate.Value,
                    DateTimeKind.Utc)
                : null,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        _db.Workers.Add(worker);
        await _db.SaveChangesAsync(cancellationToken);

        var response = ToResponse(worker);

        return CreatedAtAction(
            nameof(GetById),
            new { farmId, workerId = worker.Id },
            response);
    }

    // PUT: api/farms/{farmId}/workers/{workerId}
    [HttpPut("{workerId:guid}")]
    [FarmPermission("workers.manage")]
    public async Task<ActionResult<WorkerResponse>> Update(
        Guid farmId,
        Guid workerId,
        UpdateWorkerRequest request,
        CancellationToken cancellationToken)
    {
        var farmExists = await _db.Farms
            .AnyAsync(f => f.Id == farmId && f.IsActive, cancellationToken);

        if (!farmExists)
            return NotFound(new { message = "Farm not found or inactive." });

        var worker = await _db.Workers
            .FirstOrDefaultAsync(
                w => w.Id == workerId && w.FarmId == farmId,
                cancellationToken);

        if (worker is null)
            return NotFound(new { message = "Worker not found." });

        var workerCode = request.WorkerCode.Trim().ToUpperInvariant();

        var duplicateCode = await _db.Workers
            .AnyAsync(
                w => w.FarmId == farmId &&
                     w.WorkerCode == workerCode &&
                     w.Id != workerId,
                cancellationToken);

        if (duplicateCode)
            return Conflict(new { message = "Another worker with this code already exists in this farm." });

        worker.FullName = request.FullName.Trim();
        worker.WorkerCode = workerCode;
        worker.PhoneNumber = Normalize(request.PhoneNumber);
        worker.Email = Normalize(request.Email)?.ToLowerInvariant();
        worker.IsApplicator = request.IsApplicator;
        worker.CertificationNumber = Normalize(request.CertificationNumber);
        worker.CertificationExpiryDate = request.CertificationExpiryDate.HasValue
            ? DateTime.SpecifyKind(
                request.CertificationExpiryDate.Value,
                DateTimeKind.Utc)
            : null;

        await _db.SaveChangesAsync(cancellationToken);

        return Ok(ToResponse(worker));
    }

    // PATCH: api/farms/{farmId}/workers/{workerId}/status
    [HttpPatch("{workerId:guid}/status")]
    [FarmPermission("workers.manage")]
    public async Task<ActionResult<WorkerResponse>> UpdateStatus(
        Guid farmId,
        Guid workerId,
        UpdateWorkerStatusRequest request,
        CancellationToken cancellationToken)
    {
        var farmExists = await _db.Farms
            .AnyAsync(f => f.Id == farmId && f.IsActive, cancellationToken);

        if (!farmExists)
            return NotFound(new { message = "Farm not found or inactive." });

        var worker = await _db.Workers
            .FirstOrDefaultAsync(
                w => w.Id == workerId && w.FarmId == farmId,
                cancellationToken);

        if (worker is null)
            return NotFound(new { message = "Worker not found." });

        worker.IsActive = request.IsActive;

        await _db.SaveChangesAsync(cancellationToken);

        return Ok(ToResponse(worker));
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }

    private static WorkerResponse ToResponse(Worker worker)
    {
        return new WorkerResponse
        {
            Id = worker.Id,
            FarmId = worker.FarmId,
            FullName = worker.FullName,
            WorkerCode = worker.WorkerCode,
            PhoneNumber = worker.PhoneNumber,
            Email = worker.Email,
            IsApplicator = worker.IsApplicator,
            CertificationNumber = worker.CertificationNumber,
            CertificationExpiryDate = worker.CertificationExpiryDate,
            IsActive = worker.IsActive,
            CreatedAtUtc = worker.CreatedAtUtc
        };
    }
}

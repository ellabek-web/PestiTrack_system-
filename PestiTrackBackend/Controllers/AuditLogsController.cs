using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PestiTrackBackend.Data;
using PestiTrackBackend.DTOs.Auditing;
using PestiTrackBackend.Models.Auditing;
using PestiTrackBackend.Security;
using PestiTrackBackend.Security.Authorization;

namespace PestiTrackBackend.Controllers;

[ApiController]
[Authorize]
public sealed class AuditLogsController : ControllerBase
{
    private readonly AppDbContext _db;

    public AuditLogsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet("api/farms/{farmId:guid}/audit-logs")]
    [FarmPermission(PermissionCodes.AuditView)]
    public Task<ActionResult<AuditLogListResponse>> GetFarmAuditLogs(
        Guid farmId,
        [FromQuery] AuditLogQueryRequest request,
        CancellationToken cancellationToken)
    {
        return GetAuditLogs(
            _db.AuditLogs.Where(log => log.FarmId == farmId),
            request,
            cancellationToken);
    }

    [HttpGet("api/farms/{farmId:guid}/audit-logs/{auditLogId:guid}")]
    [FarmPermission(PermissionCodes.AuditView)]
    public async Task<ActionResult<AuditLogResponse>> GetFarmAuditLog(
        Guid farmId,
        Guid auditLogId,
        CancellationToken cancellationToken)
    {
        var auditLog = await _db.AuditLogs
            .AsNoTracking()
            .SingleOrDefaultAsync(
                log =>
                    log.Id == auditLogId &&
                    log.FarmId == farmId,
                cancellationToken);

        return auditLog is null
            ? NotFound(new { message = "Audit record not found." })
            : Map(auditLog);
    }

    [HttpGet("api/admin/audit-logs")]
    [Authorize(
        Roles = SystemRoleCodes.Administrator +
            "," +
            SystemRoleCodes.Auditor)]
    public Task<ActionResult<AuditLogListResponse>> GetSystemAuditLogs(
        [FromQuery] AuditLogQueryRequest request,
        [FromQuery] Guid? farmId,
        CancellationToken cancellationToken)
    {
        var query = _db.AuditLogs.AsQueryable();

        if (farmId.HasValue)
            query = query.Where(log => log.FarmId == farmId);

        return GetAuditLogs(query, request, cancellationToken);
    }

    [HttpGet("api/admin/audit-logs/{auditLogId:guid}")]
    [Authorize(
        Roles = SystemRoleCodes.Administrator +
            "," +
            SystemRoleCodes.Auditor)]
    public async Task<ActionResult<AuditLogResponse>> GetSystemAuditLog(
        Guid auditLogId,
        CancellationToken cancellationToken)
    {
        var auditLog = await _db.AuditLogs
            .AsNoTracking()
            .SingleOrDefaultAsync(
                log => log.Id == auditLogId,
                cancellationToken);

        return auditLog is null
            ? NotFound(new { message = "Audit record not found." })
            : Map(auditLog);
    }

    private static IQueryable<AuditLog> ApplyFilters(
        IQueryable<AuditLog> query,
        AuditLogQueryRequest request)
    {
        if (request.FromUtc.HasValue)
            query = query.Where(
                log => log.OccurredAtUtc >= request.FromUtc.Value);

        if (request.ToUtc.HasValue)
            query = query.Where(
                log => log.OccurredAtUtc <= request.ToUtc.Value);

        if (request.ActorUserId.HasValue)
            query = query.Where(
                log => log.ActorUserId == request.ActorUserId.Value);

        if (!string.IsNullOrWhiteSpace(request.Action))
        {
            var action = request.Action.Trim();
            query = query.Where(log => log.Action == action);
        }

        if (!string.IsNullOrWhiteSpace(request.EntityType))
        {
            var entityType = request.EntityType.Trim();
            query = query.Where(log => log.EntityType == entityType);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();
            query = query.Where(log =>
                log.EntityId.Contains(search) ||
                (log.ActorDisplayName != null &&
                    log.ActorDisplayName.Contains(search)) ||
                (log.ActorEmail != null &&
                    log.ActorEmail.Contains(search)) ||
                (log.RequestPath != null &&
                    log.RequestPath.Contains(search)));
        }

        return query;
    }

    private static AuditLogResponse Map(AuditLog log)
    {
        Dictionary<string, AuditValueChange>? changes;

        try
        {
            changes = JsonSerializer.Deserialize<
                Dictionary<string, AuditValueChange>>(
                log.ChangesJson);
        }
        catch (JsonException)
        {
            changes = [];
        }

        return new AuditLogResponse
        {
            Id = log.Id,
            OccurredAtUtc = log.OccurredAtUtc,
            ActorUserId = log.ActorUserId,
            ActorDisplayName = log.ActorDisplayName,
            ActorEmail = log.ActorEmail,
            FarmId = log.FarmId,
            Action = log.Action,
            EntityType = log.EntityType,
            EntityId = log.EntityId,
            Changes = changes ?? [],
            HttpMethod = log.HttpMethod,
            RequestPath = log.RequestPath,
            IpAddress = log.IpAddress,
            UserAgent = log.UserAgent
        };
    }

    private async Task<ActionResult<AuditLogListResponse>> GetAuditLogs(
        IQueryable<AuditLog> query,
        AuditLogQueryRequest request,
        CancellationToken cancellationToken)
    {
        query = ApplyFilters(query.AsNoTracking(), request);
        var totalCount = await query.CountAsync(cancellationToken);
        var logs = await query
            .OrderByDescending(log => log.OccurredAtUtc)
            .ThenByDescending(log => log.Id)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        return new AuditLogListResponse
        {
            Items = logs.Select(Map).ToList(),
            Page = request.Page,
            PageSize = request.PageSize,
            TotalCount = totalCount,
            TotalPages = totalCount == 0
                ? 0
                : (int)Math.Ceiling(
                    totalCount / (double)request.PageSize)
        };
    }
}

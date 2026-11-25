using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PestiTrackBackend.Data;
using PestiTrackBackend.DTOs.Notifications;
using PestiTrackBackend.Models.Notifications;
using PestiTrackBackend.Security;
using PestiTrackBackend.Services.Authorization;
using PestiTrackBackend.Services.Notifications;

namespace PestiTrackBackend.Controllers;

[ApiController]
[Route("api/farms/{farmId:guid}/notifications")]
[Authorize]
public sealed class NotificationsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IFarmAuthorizationService _farmAuthorizationService;
    private readonly IFarmNotificationService _notificationService;
    private readonly TimeProvider _timeProvider;

    public NotificationsController(
        AppDbContext db,
        IFarmAuthorizationService farmAuthorizationService,
        IFarmNotificationService notificationService,
        TimeProvider timeProvider)
    {
        _db = db;
        _farmAuthorizationService = farmAuthorizationService;
        _notificationService = notificationService;
        _timeProvider = timeProvider;
    }

    // GET: api/farms/{farmId}/notifications
    [HttpGet]
    public async Task<ActionResult<NotificationListResponse>> GetAll(
        Guid farmId,
        [FromQuery] NotificationQueryRequest request,
        CancellationToken cancellationToken)
    {
        var access = await AuthorizeAndResolveCategoriesAsync(farmId, cancellationToken);
        if (access.ErrorResult is not null)
            return access.ErrorResult;

        await _notificationService.SyncFarmNotificationsAsync(farmId, cancellationToken);

        var allowed = access.AllowedCategories;
        var baseQuery = _db.FarmNotifications
            .AsNoTracking()
            .Where(n => n.FarmId == farmId && allowed.Contains(n.Category));

        var unreadCount = await baseQuery
            .CountAsync(n => !n.IsResolved && !n.IsRead, cancellationToken);
        var criticalCount = await baseQuery
            .CountAsync(n => !n.IsResolved && !n.IsRead && n.Severity == NotificationSeverity.Critical, cancellationToken);

        var query = baseQuery;

        if (!request.IncludeResolved)
            query = query.Where(n => !n.IsResolved);

        if (request.UnreadOnly)
            query = query.Where(n => !n.IsRead);

        if (request.Category.HasValue)
            query = query.Where(n => n.Category == request.Category.Value);

        if (request.Severity.HasValue)
            query = query.Where(n => n.Severity == request.Severity.Value);

        var totalCount = await query.CountAsync(cancellationToken);

        var rows = await query
            .OrderBy(n => n.IsResolved)
            .ThenBy(n => n.IsRead)
            .ThenByDescending(n => n.Severity)
            .ThenByDescending(n => n.LastEvaluatedAtUtc)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        return Ok(new NotificationListResponse
        {
            Page = request.Page,
            PageSize = request.PageSize,
            TotalCount = totalCount,
            TotalPages = totalCount == 0
                ? 0
                : (int)Math.Ceiling(totalCount / (double)request.PageSize),
            UnreadCount = unreadCount,
            CriticalCount = criticalCount,
            Items = rows.Select(MapToResponse).ToList()
        });
    }

    // GET: api/farms/{farmId}/notifications/unread-count
    [HttpGet("unread-count")]
    public async Task<ActionResult<NotificationSummaryResponse>> GetUnreadCount(
        Guid farmId,
        CancellationToken cancellationToken)
    {
        var access = await AuthorizeAndResolveCategoriesAsync(farmId, cancellationToken);
        if (access.ErrorResult is not null)
            return access.ErrorResult;

        await _notificationService.SyncFarmNotificationsAsync(farmId, cancellationToken);

        var allowed = access.AllowedCategories;
        var baseQuery = _db.FarmNotifications
            .AsNoTracking()
            .Where(n => n.FarmId == farmId && !n.IsResolved && !n.IsRead && allowed.Contains(n.Category));

        return Ok(new NotificationSummaryResponse
        {
            UnreadCount = await baseQuery.CountAsync(cancellationToken),
            CriticalCount = await baseQuery.CountAsync(
                n => n.Severity == NotificationSeverity.Critical,
                cancellationToken)
        });
    }

    // PATCH: api/farms/{farmId}/notifications/{notificationId}/read
    [HttpPatch("{notificationId:guid}/read")]
    public async Task<ActionResult<NotificationResponse>> MarkRead(
        Guid farmId,
        Guid notificationId,
        [FromBody] MarkNotificationReadRequest request,
        CancellationToken cancellationToken)
    {
        var access = await AuthorizeAndResolveCategoriesAsync(farmId, cancellationToken);
        if (access.ErrorResult is not null)
            return access.ErrorResult;

        var notification = await _db.FarmNotifications
            .FirstOrDefaultAsync(
                n => n.Id == notificationId &&
                     n.FarmId == farmId &&
                     access.AllowedCategories.Contains(n.Category),
                cancellationToken);

        if (notification is null)
            return NotFound(new { message = "Notification not found." });

        notification.IsRead = request.IsRead;
        notification.ReadAtUtc = request.IsRead
            ? _timeProvider.GetUtcNow().UtcDateTime
            : null;

        await _db.SaveChangesAsync(cancellationToken);

        return Ok(MapToResponse(notification));
    }

    // POST: api/farms/{farmId}/notifications/mark-all-read
    [HttpPost("mark-all-read")]
    public async Task<ActionResult<NotificationSummaryResponse>> MarkAllRead(
        Guid farmId,
        CancellationToken cancellationToken)
    {
        var access = await AuthorizeAndResolveCategoriesAsync(farmId, cancellationToken);
        if (access.ErrorResult is not null)
            return access.ErrorResult;

        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
        var unread = await _db.FarmNotifications
            .Where(n =>
                n.FarmId == farmId &&
                !n.IsResolved &&
                !n.IsRead &&
                access.AllowedCategories.Contains(n.Category))
            .ToListAsync(cancellationToken);

        foreach (var item in unread)
        {
            item.IsRead = true;
            item.ReadAtUtc = nowUtc;
        }

        if (unread.Count > 0)
            await _db.SaveChangesAsync(cancellationToken);

        return Ok(new NotificationSummaryResponse
        {
            UnreadCount = 0,
            CriticalCount = 0
        });
    }

    private async Task<(ActionResult? ErrorResult, HashSet<NotificationCategory> AllowedCategories)>
        AuthorizeAndResolveCategoriesAsync(Guid farmId, CancellationToken cancellationToken)
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdValue, out var userId))
            return (Unauthorized(), []);

        var farmExists = await _db.Farms
            .AsNoTracking()
            .AnyAsync(f => f.Id == farmId && f.IsActive, cancellationToken);

        if (!farmExists)
            return (NotFound(new { message = "Active farm not found." }), []);

        var isAdmin = User.IsInRole(SystemRoleCodes.Administrator);
        if (isAdmin)
        {
            return (null, Enum.GetValues<NotificationCategory>().ToHashSet());
        }

        var permissions = (await _farmAuthorizationService
            .GetPermissionsAsync(userId, farmId, cancellationToken))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (permissions.Count == 0)
            return (Forbid(), []);

        var allowed = new HashSet<NotificationCategory>
        {
            NotificationCategory.ProductRegistration
        };

        if (permissions.Contains(PermissionCodes.InventoryView))
        {
            allowed.Add(NotificationCategory.LowStock);
            allowed.Add(NotificationCategory.ExpiringBatch);
            allowed.Add(NotificationCategory.ExpiredBatch);
        }

        if (permissions.Contains(PermissionCodes.WorkersView) ||
            permissions.Contains(PermissionCodes.ComplianceView))
        {
            allowed.Add(NotificationCategory.ExpiringCertification);
            allowed.Add(NotificationCategory.ExpiredCertification);
        }

        if (permissions.Contains(PermissionCodes.ComplianceView) ||
            permissions.Contains(PermissionCodes.ApplicationsView))
        {
            allowed.Add(NotificationCategory.ReEntryHold);
            allowed.Add(NotificationCategory.PreHarvestConflict);
        }

        if (permissions.Contains(PermissionCodes.ComplianceView))
        {
            allowed.Add(NotificationCategory.ComplianceAlert);
        }

        return (null, allowed);
    }

    private static NotificationResponse MapToResponse(FarmNotification n) => new()
    {
        Id = n.Id,
        FarmId = n.FarmId,
        Category = n.Category.ToString(),
        Severity = n.Severity.ToString(),
        Title = n.Title,
        Message = n.Message,
        ActionPath = n.ActionPath,
        IsRead = n.IsRead,
        ReadAtUtc = n.ReadAtUtc.HasValue ? ToUtcOffset(n.ReadAtUtc.Value) : null,
        IsResolved = n.IsResolved,
        ResolvedAtUtc = n.ResolvedAtUtc.HasValue ? ToUtcOffset(n.ResolvedAtUtc.Value) : null,
        CreatedAtUtc = ToUtcOffset(n.CreatedAtUtc),
        LastEvaluatedAtUtc = ToUtcOffset(n.LastEvaluatedAtUtc)
    };

    private static DateTimeOffset ToUtcOffset(DateTime value) =>
        new(DateTime.SpecifyKind(value, DateTimeKind.Utc));
}

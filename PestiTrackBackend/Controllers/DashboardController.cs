using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PestiTrackBackend.Data;
using PestiTrackBackend.DTOs.Dashboard;
using PestiTrackBackend.Models.Applications;
using PestiTrackBackend.Models.Crops;
using PestiTrackBackend.Models.Inventory;
using PestiTrackBackend.Security;
using PestiTrackBackend.Security.Authorization;
using PestiTrackBackend.Services.Authorization;

namespace PestiTrackBackend.Controllers;

[ApiController]
[Route("api/farms/{farmId:guid}/dashboard")]
[Authorize]
public sealed class DashboardController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IFarmAuthorizationService _farmAuthorizationService;
    private readonly TimeProvider _timeProvider;

    public DashboardController(
        AppDbContext db,
        IFarmAuthorizationService farmAuthorizationService,
        TimeProvider timeProvider)
    {
        _db = db;
        _farmAuthorizationService = farmAuthorizationService;
        _timeProvider = timeProvider;
    }

    // GET: api/farms/{farmId}/dashboard
    [HttpGet]
    [FarmPermission(PermissionCodes.FarmsView)]
    public async Task<IActionResult> Get(
        Guid farmId,
        CancellationToken cancellationToken)
    {
        var userIdValue = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(userIdValue, out var userId))
            return Unauthorized();

        var farm = await _db.Farms
            .AsNoTracking()
            .Where(item =>
                item.Id == farmId &&
                item.IsActive)
            .Select(item => new
            {
                item.Id,
                item.Name,
                item.Code,
                item.Location
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (farm is null)
            return NotFound(new { message = "Active farm not found." });

        var now = _timeProvider.GetUtcNow();
        var thirtyDaysAgo = now.UtcDateTime.AddDays(-30);
        var today = DateOnly.FromDateTime(now.UtcDateTime);
        var inThirtyDays = today.AddDays(30);

        var response = new FarmDashboardResponse
        {
            FarmId = farm.Id,
            FarmName = farm.Name,
            FarmCode = farm.Code,
            Location = farm.Location,
            GeneratedAtUtc = now
        };

        if (await HasPermission(
                userId,
                farmId,
                PermissionCodes.FieldsView,
                cancellationToken))
        {
            response.VisibleSections.Add("fields");

            response.Fields = new FieldDashboardSummary
            {
                Total = await _db.Fields
                    .CountAsync(
                        field => field.FarmId == farmId,
                        cancellationToken),
                Active = await _db.Fields
                    .CountAsync(
                        field =>
                            field.FarmId == farmId &&
                            field.IsActive,
                        cancellationToken),
                TotalAreaHectares = await _db.Fields
                    .Where(field =>
                        field.FarmId == farmId &&
                        field.IsActive)
                    .SumAsync(
                        field =>
                            field.AreaHectares ?? 0m,
                        cancellationToken)
            };
        }

        if (await HasPermission(
                userId,
                farmId,
                PermissionCodes.CropsView,
                cancellationToken))
        {
            response.VisibleSections.Add("crops");

            var cropCounts = await _db.FieldCrops
                .AsNoTracking()
                .Where(crop => crop.FarmId == farmId)
                .GroupBy(crop => crop.Status)
                .Select(group => new
                {
                    Status = group.Key,
                    Count = group.Count()
                })
                .ToDictionaryAsync(
                    item => item.Status,
                    item => item.Count,
                    cancellationToken);

            response.Crops = new CropDashboardSummary
            {
                TotalAssignments = cropCounts.Values.Sum(),
                Planned = GetCount(
                    cropCounts,
                    FieldCropStatus.Planned),
                Planted = GetCount(
                    cropCounts,
                    FieldCropStatus.Planted),
                Harvested = GetCount(
                    cropCounts,
                    FieldCropStatus.Harvested),
                Cancelled = GetCount(
                    cropCounts,
                    FieldCropStatus.Cancelled)
            };
        }

        if (await HasPermission(
                userId,
                farmId,
                PermissionCodes.WorkersView,
                cancellationToken))
        {
            response.VisibleSections.Add("workers");

            var certificationEnd = inThirtyDays.ToDateTime(
                TimeOnly.MaxValue);
            var todayStart = today.ToDateTime(
                TimeOnly.MinValue);

            response.Workers = new WorkerDashboardSummary
            {
                Total = await _db.Workers
                    .CountAsync(
                        worker => worker.FarmId == farmId,
                        cancellationToken),
                Active = await _db.Workers
                    .CountAsync(
                        worker =>
                            worker.FarmId == farmId &&
                            worker.IsActive,
                        cancellationToken),
                ActiveApplicators = await _db.Workers
                    .CountAsync(
                        worker =>
                            worker.FarmId == farmId &&
                            worker.IsActive &&
                            worker.IsApplicator,
                        cancellationToken),
                ExpiredCertifications = await _db.Workers
                    .CountAsync(
                        worker =>
                            worker.FarmId == farmId &&
                            worker.IsActive &&
                            worker.IsApplicator &&
                            worker.CertificationExpiryDate.HasValue &&
                            worker.CertificationExpiryDate.Value <
                                todayStart,
                        cancellationToken),
                CertificationsExpiringWithin30Days =
                    await _db.Workers
                        .CountAsync(
                            worker =>
                                worker.FarmId == farmId &&
                                worker.IsActive &&
                                worker.IsApplicator &&
                                worker.CertificationExpiryDate.HasValue &&
                                worker.CertificationExpiryDate.Value >=
                                    todayStart &&
                                worker.CertificationExpiryDate.Value <=
                                    certificationEnd,
                            cancellationToken)
            };
        }

        if (await HasPermission(
                userId,
                farmId,
                PermissionCodes.InventoryView,
                cancellationToken))
        {
            response.VisibleSections.Add("inventory");

            var stockLines = await _db.InventoryTransactions
                .AsNoTracking()
                .Where(transaction =>
                    transaction.FarmId == farmId)
                .GroupBy(transaction => new
                {
                    transaction.ProductId,
                    transaction.BatchNumber,
                    transaction.ExpiryDate
                })
                .Select(group => new
                {
                    group.Key.ProductId,
                    group.Key.BatchNumber,
                    group.Key.ExpiryDate,
                    Quantity = group.Sum(transaction =>
                        transaction.TransactionType ==
                            InventoryTransactionType.StockIn ||
                        transaction.TransactionType ==
                            InventoryTransactionType.AdjustmentIn
                            ? transaction.Quantity
                            : -transaction.Quantity)
                })
                .Where(stock => stock.Quantity > 0)
                .ToListAsync(cancellationToken);

            response.Inventory = new InventoryDashboardSummary
            {
                StockLineCount = stockLines.Count,
                ProductCount = stockLines
                    .Select(stock => stock.ProductId)
                    .Distinct()
                    .Count(),
                BatchCount = stockLines
                    .Select(stock => new
                    {
                        stock.ProductId,
                        stock.BatchNumber
                    })
                    .Distinct()
                    .Count(),
                ExpiredBatchCount = stockLines.Count(stock =>
                    stock.ExpiryDate.HasValue &&
                    stock.ExpiryDate.Value < today),
                BatchesExpiringWithin30Days = stockLines.Count(stock =>
                    stock.ExpiryDate.HasValue &&
                    stock.ExpiryDate.Value >= today &&
                    stock.ExpiryDate.Value <= inThirtyDays)
            };

            response.RecentInventoryTransactions =
                await _db.InventoryTransactions
                    .AsNoTracking()
                    .Where(transaction =>
                        transaction.FarmId == farmId)
                    .OrderByDescending(transaction =>
                        transaction.CreatedAtUtc)
                    .ThenByDescending(transaction =>
                        transaction.Id)
                    .Take(5)
                    .Select(transaction =>
                        new RecentInventoryDashboardItem
                        {
                            Id = transaction.Id,
                            CreatedAtUtc = new DateTimeOffset(
                                DateTime.SpecifyKind(
                                    transaction.CreatedAtUtc,
                                    DateTimeKind.Utc)),
                            ProductName =
                                transaction.Product.Name,
                            TransactionType =
                                transaction.TransactionType ==
                                    InventoryTransactionType.StockIn
                                    ? "StockIn"
                                    : transaction.TransactionType ==
                                        InventoryTransactionType.StockOut
                                        ? "StockOut"
                                        : transaction.TransactionType ==
                                            InventoryTransactionType.AdjustmentIn
                                            ? "AdjustmentIn"
                                            : "AdjustmentOut",
                            Quantity = transaction.Quantity,
                            BatchNumber =
                                transaction.BatchNumber,
                            Reference = transaction.Reference
                        })
                    .ToListAsync(cancellationToken);
        }

        if (await HasPermission(
                userId,
                farmId,
                PermissionCodes.ApplicationsView,
                cancellationToken))
        {
            response.VisibleSections.Add("applications");

            var applicationCounts = await _db.PesticideApplications
                .AsNoTracking()
                .Where(application =>
                    application.FarmId == farmId)
                .GroupBy(application => application.Status)
                .Select(group => new
                {
                    Status = group.Key,
                    Count = group.Count()
                })
                .ToDictionaryAsync(
                    item => item.Status,
                    item => item.Count,
                    cancellationToken);

            var completedWithin30Days =
                _db.PesticideApplications
                    .AsNoTracking()
                    .Where(application =>
                        application.FarmId == farmId &&
                        application.Status ==
                            PesticideApplicationStatus.Completed &&
                        application.ApplicationDateUtc >=
                            thirtyDaysAgo);

            response.Applications =
                new ApplicationDashboardSummary
                {
                    Total = applicationCounts.Values.Sum(),
                    Draft = GetCount(
                        applicationCounts,
                        PesticideApplicationStatus.Draft),
                    Completed = GetCount(
                        applicationCounts,
                        PesticideApplicationStatus.Completed),
                    CompletedWithin30Days =
                        await completedWithin30Days.CountAsync(
                            cancellationToken),
                    AreaTreatedWithin30DaysHectares =
                        await completedWithin30Days.SumAsync(
                            application =>
                                application.AreaTreatedHectares,
                            cancellationToken)
                };

            response.RecentApplications =
                await _db.PesticideApplications
                    .AsNoTracking()
                    .Where(application =>
                        application.FarmId == farmId)
                    .OrderByDescending(application =>
                        application.ApplicationDateUtc)
                    .ThenByDescending(application =>
                        application.CreatedAtUtc)
                    .Take(5)
                    .Select(application =>
                        new RecentApplicationDashboardItem
                        {
                            Id = application.Id,
                            ApplicationDateUtc =
                                new DateTimeOffset(
                                    DateTime.SpecifyKind(
                                        application.ApplicationDateUtc,
                                        DateTimeKind.Utc)),
                            FieldName =
                                application.Field.Name,
                            CropName =
                                application.FieldCrop.Crop.Name,
                            WorkerName =
                                application.Worker.FullName,
                            TargetPest =
                                application.TargetPest,
                            Status =
                                application.Status ==
                                    PesticideApplicationStatus.Draft
                                    ? "Draft"
                                    : "Completed"
                        })
                    .ToListAsync(cancellationToken);
        }

        return Ok(response);
    }

    private Task<bool> HasPermission(
        Guid userId,
        Guid farmId,
        string permissionCode,
        CancellationToken cancellationToken)
    {
        return _farmAuthorizationService.HasPermissionAsync(
            userId,
            farmId,
            permissionCode,
            cancellationToken);
    }

    private static int GetCount<TStatus>(
        IReadOnlyDictionary<TStatus, int> counts,
        TStatus status)
        where TStatus : notnull
    {
        return counts.TryGetValue(status, out var count)
            ? count
            : 0;
    }
}

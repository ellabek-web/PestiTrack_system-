using Microsoft.EntityFrameworkCore;
using PestiTrackBackend.Data;
using PestiTrackBackend.Models.Applications;
using PestiTrackBackend.Models.Compliance;
using PestiTrackBackend.Models.Crops;
using PestiTrackBackend.Models.Inventory;
using PestiTrackBackend.Models.Notifications;

namespace PestiTrackBackend.Services.Notifications;

public interface IFarmNotificationService
{
    Task SyncFarmNotificationsAsync(
        Guid farmId,
        CancellationToken cancellationToken = default);
}

public sealed class FarmNotificationService : IFarmNotificationService
{
    private readonly AppDbContext _db;
    private readonly TimeProvider _timeProvider;

    public FarmNotificationService(
        AppDbContext db,
        TimeProvider timeProvider)
    {
        _db = db;
        _timeProvider = timeProvider;
    }

    public async Task SyncFarmNotificationsAsync(
        Guid farmId,
        CancellationToken cancellationToken = default)
    {
        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
        var today = DateOnly.FromDateTime(nowUtc);
        var desired = new List<DesiredAlert>();

        // 1. Inventory balances (for low-stock thresholds and expiring/expired batches).
        var batchBalances = await _db.InventoryTransactions
            .AsNoTracking()
            .Where(t => t.FarmId == farmId)
            .GroupBy(t => new
            {
                t.ProductId,
                ProductName = t.Product.Name,
                t.BatchNumber,
                t.ExpiryDate
            })
            .Select(g => new
            {
                g.Key.ProductId,
                g.Key.ProductName,
                BatchNumber = g.Key.BatchNumber ?? string.Empty,
                g.Key.ExpiryDate,
                QuantityOnHand = g.Sum(t =>
                    t.TransactionType == InventoryTransactionType.StockIn ||
                    t.TransactionType == InventoryTransactionType.AdjustmentIn
                        ? t.Quantity
                        : -t.Quantity)
            })
            .Where(b => b.QuantityOnHand > 0)
            .ToListAsync(cancellationToken);

        var productTotals = batchBalances
            .GroupBy(b => b.ProductId)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.QuantityOnHand));

        var thresholds = await _db.InventoryThresholds
            .AsNoTracking()
            .Include(t => t.Product)
            .Where(t => t.FarmId == farmId && t.Product.IsActive)
            .ToListAsync(cancellationToken);

        foreach (var threshold in thresholds)
        {
            var onHand = productTotals.GetValueOrDefault(threshold.ProductId, 0m);
            if (onHand < threshold.MinimumQuantity)
            {
                desired.Add(new DesiredAlert(
                    $"low-stock:{threshold.ProductId:N}",
                    NotificationCategory.LowStock,
                    onHand <= 0 ? NotificationSeverity.Critical : NotificationSeverity.Warning,
                    $"Low stock: {threshold.Product.Name}",
                    $"{threshold.Product.Name} is at {onHand:0.###} (below minimum threshold of {threshold.MinimumQuantity:0.###}).",
                    "/inventory"));
            }
        }

        // 2. Expiring / expired inventory batches (within 30 days).
        var batchCutoff = today.AddDays(30);
        foreach (var batch in batchBalances.Where(b => b.ExpiryDate.HasValue && b.ExpiryDate.Value <= batchCutoff))
        {
            var expiry = batch.ExpiryDate!.Value;
            if (expiry < today)
            {
                desired.Add(new DesiredAlert(
                    $"expired-batch:{batch.ProductId:N}:{batch.BatchNumber.ToLowerInvariant()}",
                    NotificationCategory.ExpiredBatch,
                    NotificationSeverity.Critical,
                    $"Expired batch: {batch.ProductName}",
                    $"Batch '{batch.BatchNumber}' ({batch.QuantityOnHand:0.###} on hand) expired on {expiry:yyyy-MM-dd}.",
                    "/inventory"));
            }
            else
            {
                var days = expiry.DayNumber - today.DayNumber;
                desired.Add(new DesiredAlert(
                    $"expiring-batch:{batch.ProductId:N}:{batch.BatchNumber.ToLowerInvariant()}",
                    NotificationCategory.ExpiringBatch,
                    NotificationSeverity.Warning,
                    $"Expiring batch: {batch.ProductName}",
                    $"Batch '{batch.BatchNumber}' ({batch.QuantityOnHand:0.###} on hand) expires in {days} day(s) on {expiry:yyyy-MM-dd}.",
                    "/inventory"));
            }
        }

        // 3. Applicator certification expiry alerts (within 30 days).
        var applicators = await _db.Workers
            .AsNoTracking()
            .Where(w => w.FarmId == farmId && w.IsActive && w.IsApplicator)
            .ToListAsync(cancellationToken);

        foreach (var worker in applicators)
        {
            if (string.IsNullOrWhiteSpace(worker.CertificationNumber) || !worker.CertificationExpiryDate.HasValue)
            {
                desired.Add(new DesiredAlert(
                    $"cert-expired:{worker.Id:N}",
                    NotificationCategory.ExpiredCertification,
                    NotificationSeverity.Critical,
                    $"Missing certification: {worker.FullName}",
                    $"{worker.FullName} ({worker.WorkerCode}) is marked as an applicator but has no valid certification on file.",
                    "/workers"));
                continue;
            }

            var expiry = DateOnly.FromDateTime(worker.CertificationExpiryDate.Value);
            var days = expiry.DayNumber - today.DayNumber;
            if (days < 0)
            {
                desired.Add(new DesiredAlert(
                    $"cert-expired:{worker.Id:N}",
                    NotificationCategory.ExpiredCertification,
                    NotificationSeverity.Critical,
                    $"Expired certification: {worker.FullName}",
                    $"{worker.FullName}'s applicator certification ({worker.CertificationNumber}) expired on {expiry:yyyy-MM-dd}.",
                    "/workers"));
            }
            else if (days <= 30)
            {
                desired.Add(new DesiredAlert(
                    $"cert-expiring:{worker.Id:N}",
                    NotificationCategory.ExpiringCertification,
                    NotificationSeverity.Warning,
                    $"Expiring certification: {worker.FullName}",
                    $"{worker.FullName}'s applicator certification expires in {days} day(s) on {expiry:yyyy-MM-dd}.",
                    "/workers"));
            }
        }

        // 4. Active REI holds & PHI harvest conflicts.
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
                FieldName = a.Field.Name,
                CropName = a.FieldCrop.Crop.Name,
                CropStatus = a.FieldCrop.Status,
                a.FieldCrop.ExpectedHarvestDate,
                a.ApplicationDateUtc,
                Products = a.Products.Select(p => new
                {
                    p.Product.ReEntryIntervalHours,
                    p.Product.PreHarvestIntervalDays
                }).ToList()
            })
            .ToListAsync(cancellationToken);

        foreach (var app in completedApps)
        {
            var maxReiHours = app.Products
                .Where(p => p.ReEntryIntervalHours.HasValue && p.ReEntryIntervalHours.Value > 0)
                .Select(p => p.ReEntryIntervalHours!.Value)
                .DefaultIfEmpty(0)
                .Max();

            if (maxReiHours > 0)
            {
                var allowedAt = DateTime.SpecifyKind(
                    app.ApplicationDateUtc.AddHours(maxReiHours),
                    DateTimeKind.Utc);
                if (allowedAt > nowUtc)
                {
                    desired.Add(new DesiredAlert(
                        $"rei-hold:{app.Id:N}",
                        NotificationCategory.ReEntryHold,
                        NotificationSeverity.Warning,
                        $"Active REI hold: {app.FieldName}",
                        $"Re-entry to {app.FieldName} ({app.CropName}) is restricted until {allowedAt:yyyy-MM-dd HH:mm} UTC.",
                        "/compliance"));
                }
            }

            var maxPhiDays = app.Products
                .Where(p => p.PreHarvestIntervalDays.HasValue && p.PreHarvestIntervalDays.Value > 0)
                .Select(p => p.PreHarvestIntervalDays!.Value)
                .DefaultIfEmpty(0)
                .Max();

            if (app.CropStatus == FieldCropStatus.Planted &&
                maxPhiDays > 0 &&
                app.ExpectedHarvestDate.HasValue)
            {
                var earliestHarvest = DateOnly.FromDateTime(app.ApplicationDateUtc).AddDays(maxPhiDays);
                if (earliestHarvest > today && app.ExpectedHarvestDate.Value < earliestHarvest)
                {
                    desired.Add(new DesiredAlert(
                        $"phi-conflict:{app.Id:N}",
                        NotificationCategory.PreHarvestConflict,
                        NotificationSeverity.Critical,
                        $"PHI harvest conflict: {app.FieldName}",
                        $"{app.CropName} on {app.FieldName} has expected harvest {app.ExpectedHarvestDate.Value:yyyy-MM-dd}, earlier than safe PHI date {earliestHarvest:yyyy-MM-dd}.",
                        "/compliance"));
                }
            }
        }

        // 5. Product registration expiry alerts (within 60 days).
        var regCutoff = today.AddDays(60);
        var expiringProducts = await _db.Products
            .AsNoTracking()
            .Where(p =>
                p.IsActive &&
                p.RegistrationExpiryDate.HasValue &&
                p.RegistrationExpiryDate.Value <= regCutoff)
            .ToListAsync(cancellationToken);

        foreach (var product in expiringProducts)
        {
            var expiry = product.RegistrationExpiryDate!.Value;
            var days = expiry.DayNumber - today.DayNumber;
            desired.Add(new DesiredAlert(
                $"product-reg:{product.Id:N}",
                NotificationCategory.ProductRegistration,
                days < 0 ? NotificationSeverity.Critical : NotificationSeverity.Warning,
                days < 0
                    ? $"Expired product registration: {product.Name}"
                    : $"Expiring product registration: {product.Name}",
                days < 0
                    ? $"{product.Name} registration expired on {expiry:yyyy-MM-dd}."
                    : $"{product.Name} registration expires in {days} day(s) on {expiry:yyyy-MM-dd}.",
                "/products"));
        }

        // 6. Non-compliant or overdue compliance records.
        var flaggedCompliance = await _db.ComplianceRecords
            .AsNoTracking()
            .Where(r =>
                r.FarmId == farmId &&
                (r.Status == ComplianceRecordStatus.NonCompliant ||
                 (r.Status != ComplianceRecordStatus.Resolved &&
                  r.DueDateUtc.HasValue &&
                  r.DueDateUtc.Value < nowUtc)))
            .ToListAsync(cancellationToken);

        foreach (var record in flaggedCompliance)
        {
            var isNonCompliant = record.Status == ComplianceRecordStatus.NonCompliant;
            desired.Add(new DesiredAlert(
                $"compliance:{record.Id:N}",
                NotificationCategory.ComplianceAlert,
                isNonCompliant || record.Severity >= ComplianceSeverity.High
                    ? NotificationSeverity.Critical
                    : NotificationSeverity.Warning,
                isNonCompliant
                    ? $"Non-compliant finding: {record.Title}"
                    : $"Overdue compliance action: {record.Title}",
                isNonCompliant
                    ? $"Compliance record '{record.Title}' ({record.Severity}) is marked non-compliant."
                    : $"Compliance record '{record.Title}' was due on {record.DueDateUtc:yyyy-MM-dd}.",
                "/compliance"));
        }

        // Upsert active notifications and auto-resolve cleared ones.
        var existing = await _db.FarmNotifications
            .Where(n => n.FarmId == farmId)
            .ToListAsync(cancellationToken);

        var existingByKey = existing.ToDictionary(
            n => n.DeduplicationKey,
            StringComparer.OrdinalIgnoreCase);

        var activeKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var changed = false;

        foreach (var alert in desired)
        {
            activeKeys.Add(alert.Key);

            if (existingByKey.TryGetValue(alert.Key, out var current))
            {
                var escalated = alert.Severity > current.Severity || current.IsResolved;
                if (current.Category != alert.Category ||
                    current.Severity != alert.Severity ||
                    current.Title != alert.Title ||
                    current.Message != alert.Message ||
                    current.ActionPath != alert.ActionPath ||
                    current.IsResolved)
                {
                    current.Category = alert.Category;
                    current.Severity = alert.Severity;
                    current.Title = alert.Title;
                    current.Message = alert.Message;
                    current.ActionPath = alert.ActionPath;
                    current.IsResolved = false;
                    current.ResolvedAtUtc = null;
                    if (escalated)
                    {
                        current.IsRead = false;
                        current.ReadAtUtc = null;
                    }
                    current.LastEvaluatedAtUtc = nowUtc;
                    changed = true;
                }
            }
            else
            {
                _db.FarmNotifications.Add(new FarmNotification
                {
                    Id = Guid.NewGuid(),
                    FarmId = farmId,
                    DeduplicationKey = alert.Key,
                    Category = alert.Category,
                    Severity = alert.Severity,
                    Title = alert.Title,
                    Message = alert.Message,
                    ActionPath = alert.ActionPath,
                    IsRead = false,
                    IsResolved = false,
                    CreatedAtUtc = nowUtc,
                    LastEvaluatedAtUtc = nowUtc
                });
                changed = true;
            }
        }

        foreach (var stale in existing.Where(n => !n.IsResolved && !activeKeys.Contains(n.DeduplicationKey)))
        {
            stale.IsResolved = true;
            stale.ResolvedAtUtc = nowUtc;
            stale.LastEvaluatedAtUtc = nowUtc;
            changed = true;
        }

        if (changed)
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
    }

    private sealed record DesiredAlert(
        string Key,
        NotificationCategory Category,
        NotificationSeverity Severity,
        string Title,
        string Message,
        string ActionPath);
}

using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PestiTrackBackend.Data;
using PestiTrackBackend.DTOs.Reports;
using PestiTrackBackend.Models.Applications;
using PestiTrackBackend.Models.Inventory;
using PestiTrackBackend.Security;
using PestiTrackBackend.Security.Authorization;
using PestiTrackBackend.Services.Exports;

namespace PestiTrackBackend.Controllers;

[ApiController]
[Route("api/farms/{farmId:guid}/reports")]
[Authorize]
public sealed class ReportsController : ControllerBase
{
    private const int MaximumExportRows = 10_000;

    private readonly AppDbContext _db;
    private readonly TimeProvider _timeProvider;

    public ReportsController(
        AppDbContext db,
        TimeProvider timeProvider)
    {
        _db = db;
        _timeProvider = timeProvider;
    }

    // GET: api/farms/{farmId}/reports/applications
    [HttpGet("applications")]
    [FarmPermission(PermissionCodes.ReportsView)]
    public async Task<IActionResult> GetApplications(
        Guid farmId,
        [FromQuery] ApplicationReportQueryRequest request,
        CancellationToken cancellationToken)
    {
        var validationError = ValidateDateRange(
            request.FromUtc,
            request.ToUtc);

        if (validationError is not null)
            return BadRequest(new { message = validationError });

        if (!await FarmExists(farmId, cancellationToken))
            return NotFound(new { message = "Active farm not found." });

        var query = ApplyApplicationFilters(
            farmId,
            request);
        var totalCount = await query.CountAsync(cancellationToken);

        var statusCounts = await query
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

        var totalArea = await query.SumAsync(
            application => application.AreaTreatedHectares,
            cancellationToken);

        var items = await ProjectApplications(query)
            .OrderByDescending(item => item.ApplicationDateUtc)
            .ThenByDescending(item => item.Id)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        return Ok(new ApplicationReportResponse
        {
            GeneratedAtUtc = _timeProvider.GetUtcNow(),
            Page = request.Page,
            PageSize = request.PageSize,
            TotalCount = totalCount,
            TotalPages = CalculateTotalPages(
                totalCount,
                request.PageSize),
            DraftCount = GetCount(
                statusCounts,
                PesticideApplicationStatus.Draft),
            CompletedCount = GetCount(
                statusCounts,
                PesticideApplicationStatus.Completed),
            TotalAreaTreatedHectares = totalArea,
            Items = items
        });
    }

    // GET: api/farms/{farmId}/reports/inventory
    [HttpGet("inventory")]
    [FarmPermission(PermissionCodes.ReportsView)]
    public async Task<IActionResult> GetInventory(
        Guid farmId,
        [FromQuery] InventoryReportQueryRequest request,
        CancellationToken cancellationToken)
    {
        var validationError = ValidateDateRange(
            request.FromUtc,
            request.ToUtc);

        if (validationError is not null)
            return BadRequest(new { message = validationError });

        if (!await FarmExists(farmId, cancellationToken))
            return NotFound(new { message = "Active farm not found." });

        var query = ApplyInventoryFilters(
            farmId,
            request);
        var totalCount = await query.CountAsync(cancellationToken);

        var typeCounts = await query
            .GroupBy(transaction => transaction.TransactionType)
            .Select(group => new
            {
                Type = group.Key,
                Count = group.Count()
            })
            .ToDictionaryAsync(
                item => item.Type,
                item => item.Count,
                cancellationToken);

        var items = await ProjectInventory(query)
            .OrderByDescending(item => item.CreatedAtUtc)
            .ThenByDescending(item => item.Id)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        return Ok(new InventoryReportResponse
        {
            GeneratedAtUtc = _timeProvider.GetUtcNow(),
            Page = request.Page,
            PageSize = request.PageSize,
            TotalCount = totalCount,
            TotalPages = CalculateTotalPages(
                totalCount,
                request.PageSize),
            StockInCount = GetCount(
                typeCounts,
                InventoryTransactionType.StockIn),
            StockOutCount = GetCount(
                typeCounts,
                InventoryTransactionType.StockOut),
            AdjustmentInCount = GetCount(
                typeCounts,
                InventoryTransactionType.AdjustmentIn),
            AdjustmentOutCount = GetCount(
                typeCounts,
                InventoryTransactionType.AdjustmentOut),
            Items = items
        });
    }

    // GET: api/farms/{farmId}/reports/product-usage
    [HttpGet("product-usage")]
    [FarmPermission(PermissionCodes.ReportsView)]
    public async Task<IActionResult> GetProductUsage(
        Guid farmId,
        [FromQuery] ApplicationReportQueryRequest request,
        CancellationToken cancellationToken)
    {
        var validationError = ValidateDateRange(
            request.FromUtc,
            request.ToUtc);

        if (validationError is not null)
            return BadRequest(new { message = validationError });

        if (!await FarmExists(farmId, cancellationToken))
            return NotFound(new { message = "Active farm not found." });

        var applications = ApplyApplicationFilters(
                farmId,
                request)
            .Where(application =>
                application.Status ==
                    PesticideApplicationStatus.Completed);

        var usageRows = await applications
            .SelectMany(
                application => application.Products,
                (application, product) => new
                {
                    application.Id,
                    application.ApplicationDateUtc,
                    product.ProductId,
                    ProductName = product.Product.Name,
                    product.QuantityUsed
                })
            .GroupBy(item => new
            {
                item.ProductId,
                item.ProductName
            })
            .Select(group => new
            {
                ProductId = group.Key.ProductId,
                ProductName = group.Key.ProductName,
                ApplicationCount = group
                    .Select(item => item.Id)
                    .Distinct()
                    .Count(),
                TotalQuantityUsed = group.Sum(item =>
                    item.QuantityUsed),
                FirstApplicationDateUtc = group.Min(item =>
                    item.ApplicationDateUtc),
                LastApplicationDateUtc = group.Max(item =>
                    item.ApplicationDateUtc)
            })
            .OrderByDescending(item =>
                item.TotalQuantityUsed)
            .ThenBy(item => item.ProductName)
            .ToListAsync(cancellationToken);

        var items = usageRows
            .Select(item => new ProductUsageReportItem
            {
                ProductId = item.ProductId,
                ProductName = item.ProductName,
                ApplicationCount = item.ApplicationCount,
                TotalQuantityUsed = item.TotalQuantityUsed,
                FirstApplicationDateUtc = ToUtcOffset(
                    item.FirstApplicationDateUtc),
                LastApplicationDateUtc = ToUtcOffset(
                    item.LastApplicationDateUtc)
            })
            .ToList();

        return Ok(new ProductUsageReportResponse
        {
            GeneratedAtUtc = _timeProvider.GetUtcNow(),
            TotalProducts = items.Count,
            Items = items
        });
    }

    // GET: api/farms/{farmId}/reports/worker-activity
    [HttpGet("worker-activity")]
    [FarmPermission(PermissionCodes.ReportsView)]
    public async Task<IActionResult> GetWorkerActivity(
        Guid farmId,
        [FromQuery] ApplicationReportQueryRequest request,
        CancellationToken cancellationToken)
    {
        var validationError = ValidateDateRange(
            request.FromUtc,
            request.ToUtc);

        if (validationError is not null)
            return BadRequest(new { message = validationError });

        if (!await FarmExists(farmId, cancellationToken))
            return NotFound(new { message = "Active farm not found." });

        var applications = ApplyApplicationFilters(
            farmId,
            request);

        var activityRows = await applications
            .GroupBy(application => new
            {
                application.WorkerId,
                application.Worker.FullName,
                application.Worker.WorkerCode,
                application.Worker.IsActive
            })
            .Select(group => new
            {
                WorkerId = group.Key.WorkerId,
                WorkerName = group.Key.FullName,
                WorkerCode = group.Key.WorkerCode,
                IsActive = group.Key.IsActive,
                ApplicationCount = group.Count(),
                CompletedApplicationCount = group.Count(
                    application =>
                        application.Status ==
                            PesticideApplicationStatus.Completed),
                TotalAreaTreatedHectares = group.Sum(
                    application =>
                        application.AreaTreatedHectares),
                LastApplicationDateUtc = group.Max(application =>
                    application.ApplicationDateUtc)
            })
            .OrderByDescending(item =>
                item.ApplicationCount)
            .ThenBy(item => item.WorkerName)
            .ToListAsync(cancellationToken);

        var items = activityRows
            .Select(item => new WorkerActivityReportItem
            {
                WorkerId = item.WorkerId,
                WorkerName = item.WorkerName,
                WorkerCode = item.WorkerCode,
                IsActive = item.IsActive,
                ApplicationCount = item.ApplicationCount,
                CompletedApplicationCount =
                    item.CompletedApplicationCount,
                TotalAreaTreatedHectares =
                    item.TotalAreaTreatedHectares,
                LastApplicationDateUtc = ToUtcOffset(
                    item.LastApplicationDateUtc)
            })
            .ToList();

        return Ok(new WorkerActivityReportResponse
        {
            GeneratedAtUtc = _timeProvider.GetUtcNow(),
            TotalWorkers = items.Count,
            Items = items
        });
    }

    // GET: api/farms/{farmId}/reports/applications/export/csv
    [HttpGet("applications/export/csv")]
    [FarmPermission(PermissionCodes.ReportsView)]
    [FarmPermission(PermissionCodes.ReportsExport)]
    public async Task<IActionResult> ExportApplicationsCsv(
        Guid farmId,
        [FromQuery] ApplicationReportQueryRequest request,
        CancellationToken cancellationToken)
    {
        var validationError = ValidateDateRange(
            request.FromUtc,
            request.ToUtc);

        if (validationError is not null)
            return BadRequest(new { message = validationError });

        if (!await FarmExists(farmId, cancellationToken))
            return NotFound(new { message = "Active farm not found." });

        var query = ApplyApplicationFilters(
            farmId,
            request);
        var items = await ProjectApplications(query)
            .OrderByDescending(item => item.ApplicationDateUtc)
            .Take(MaximumExportRows + 1)
            .ToListAsync(cancellationToken);

        if (items.Count > MaximumExportRows)
            return ExportLimitExceeded();

        var content = CsvExportWriter.Write(
            [
                "Application ID",
                "Application date UTC",
                "Field",
                "Crop",
                "Worker",
                "Target pest",
                "Method",
                "Area treated hectares",
                "Status",
                "Product count"
            ],
            items.Select(item => (IReadOnlyList<string?>)
            [
                item.Id.ToString(),
                item.ApplicationDateUtc.ToString("O"),
                item.FieldName,
                item.CropName,
                item.WorkerName,
                item.TargetPest,
                item.ApplicationMethod,
                FormatDecimal(item.AreaTreatedHectares),
                item.Status,
                item.ProductCount.ToString(
                    CultureInfo.InvariantCulture)
            ]));

        return CsvFile(
            content,
            $"applications-{farmId:N}.csv");
    }

    // GET: api/farms/{farmId}/reports/inventory/export/csv
    [HttpGet("inventory/export/csv")]
    [FarmPermission(PermissionCodes.ReportsView)]
    [FarmPermission(PermissionCodes.ReportsExport)]
    public async Task<IActionResult> ExportInventoryCsv(
        Guid farmId,
        [FromQuery] InventoryReportQueryRequest request,
        CancellationToken cancellationToken)
    {
        var validationError = ValidateDateRange(
            request.FromUtc,
            request.ToUtc);

        if (validationError is not null)
            return BadRequest(new { message = validationError });

        if (!await FarmExists(farmId, cancellationToken))
            return NotFound(new { message = "Active farm not found." });

        var items = await ProjectInventory(
                ApplyInventoryFilters(farmId, request))
            .OrderByDescending(item => item.CreatedAtUtc)
            .Take(MaximumExportRows + 1)
            .ToListAsync(cancellationToken);

        if (items.Count > MaximumExportRows)
            return ExportLimitExceeded();

        var content = CsvExportWriter.Write(
            [
                "Transaction ID",
                "Created at UTC",
                "Product",
                "Transaction type",
                "Quantity",
                "Batch",
                "Expiry date",
                "Reference",
                "Reason"
            ],
            items.Select(item => (IReadOnlyList<string?>)
            [
                item.Id.ToString(),
                item.CreatedAtUtc.ToString("O"),
                item.ProductName,
                item.TransactionType,
                FormatDecimal(item.Quantity),
                item.BatchNumber,
                item.ExpiryDate?.ToString("yyyy-MM-dd"),
                item.Reference,
                item.Reason
            ]));

        return CsvFile(
            content,
            $"inventory-{farmId:N}.csv");
    }

    // GET: api/farms/{farmId}/reports/product-usage/export/csv
    [HttpGet("product-usage/export/csv")]
    [FarmPermission(PermissionCodes.ReportsView)]
    [FarmPermission(PermissionCodes.ReportsExport)]
    public async Task<IActionResult> ExportProductUsageCsv(
        Guid farmId,
        [FromQuery] ApplicationReportQueryRequest request,
        CancellationToken cancellationToken)
    {
        var reportResult = await GetProductUsage(
            farmId,
            request,
            cancellationToken);

        if (reportResult is not OkObjectResult ok ||
            ok.Value is not ProductUsageReportResponse report)
        {
            return reportResult;
        }

        var content = CsvExportWriter.Write(
            [
                "Product ID",
                "Product",
                "Application count",
                "Total quantity used",
                "First application UTC",
                "Last application UTC"
            ],
            report.Items.Select(item => (IReadOnlyList<string?>)
            [
                item.ProductId.ToString(),
                item.ProductName,
                item.ApplicationCount.ToString(
                    CultureInfo.InvariantCulture),
                FormatDecimal(item.TotalQuantityUsed),
                item.FirstApplicationDateUtc?.ToString("O"),
                item.LastApplicationDateUtc?.ToString("O")
            ]));

        return CsvFile(
            content,
            $"product-usage-{farmId:N}.csv");
    }

    // GET: api/farms/{farmId}/reports/worker-activity/export/csv
    [HttpGet("worker-activity/export/csv")]
    [FarmPermission(PermissionCodes.ReportsView)]
    [FarmPermission(PermissionCodes.ReportsExport)]
    public async Task<IActionResult> ExportWorkerActivityCsv(
        Guid farmId,
        [FromQuery] ApplicationReportQueryRequest request,
        CancellationToken cancellationToken)
    {
        var reportResult = await GetWorkerActivity(
            farmId,
            request,
            cancellationToken);

        if (reportResult is not OkObjectResult ok ||
            ok.Value is not WorkerActivityReportResponse report)
        {
            return reportResult;
        }

        var content = CsvExportWriter.Write(
            [
                "Worker ID",
                "Worker",
                "Worker code",
                "Active",
                "Application count",
                "Completed count",
                "Total area hectares",
                "Last application UTC"
            ],
            report.Items.Select(item => (IReadOnlyList<string?>)
            [
                item.WorkerId.ToString(),
                item.WorkerName,
                item.WorkerCode,
                item.IsActive ? "Yes" : "No",
                item.ApplicationCount.ToString(
                    CultureInfo.InvariantCulture),
                item.CompletedApplicationCount.ToString(
                    CultureInfo.InvariantCulture),
                FormatDecimal(item.TotalAreaTreatedHectares),
                item.LastApplicationDateUtc?.ToString("O")
            ]));

        return CsvFile(
            content,
            $"worker-activity-{farmId:N}.csv");
    }

    // GET: api/farms/{farmId}/reports/applications/export/{format}
    [HttpGet("applications/export/{format:alpha}")]
    [FarmPermission(PermissionCodes.ReportsView)]
    [FarmPermission(PermissionCodes.ReportsExport)]
    public async Task<IActionResult> ExportApplicationsDocument(
        Guid farmId,
        string format,
        [FromQuery] ApplicationReportQueryRequest request,
        CancellationToken cancellationToken)
    {
        var validationError = ValidateDateRange(
            request.FromUtc,
            request.ToUtc);

        if (validationError is not null)
            return BadRequest(new { message = validationError });

        if (!await FarmExists(farmId, cancellationToken))
            return NotFound(new { message = "Active farm not found." });

        var document = await BuildApplicationDocument(
            farmId,
            request,
            cancellationToken);

        return document is null
            ? ExportLimitExceeded()
            : ExportDocument(
                document,
                format,
                $"applications-{farmId:N}");
    }

    // GET: api/farms/{farmId}/reports/inventory/export/{format}
    [HttpGet("inventory/export/{format:alpha}")]
    [FarmPermission(PermissionCodes.ReportsView)]
    [FarmPermission(PermissionCodes.ReportsExport)]
    public async Task<IActionResult> ExportInventoryDocument(
        Guid farmId,
        string format,
        [FromQuery] InventoryReportQueryRequest request,
        CancellationToken cancellationToken)
    {
        var validationError = ValidateDateRange(
            request.FromUtc,
            request.ToUtc);

        if (validationError is not null)
            return BadRequest(new { message = validationError });

        if (!await FarmExists(farmId, cancellationToken))
            return NotFound(new { message = "Active farm not found." });

        var document = await BuildInventoryDocument(
            farmId,
            request,
            cancellationToken);

        return document is null
            ? ExportLimitExceeded()
            : ExportDocument(
                document,
                format,
                $"inventory-{farmId:N}");
    }

    // GET: api/farms/{farmId}/reports/product-usage/export/{format}
    [HttpGet("product-usage/export/{format:alpha}")]
    [FarmPermission(PermissionCodes.ReportsView)]
    [FarmPermission(PermissionCodes.ReportsExport)]
    public async Task<IActionResult> ExportProductUsageDocument(
        Guid farmId,
        string format,
        [FromQuery] ApplicationReportQueryRequest request,
        CancellationToken cancellationToken)
    {
        var reportResult = await GetProductUsage(
            farmId,
            request,
            cancellationToken);

        if (reportResult is not OkObjectResult ok ||
            ok.Value is not ProductUsageReportResponse report)
        {
            return reportResult;
        }

        if (report.Items.Count > MaximumExportRows)
            return ExportLimitExceeded();

        var document = new TabularExportDocument
        {
            Title = "Product usage report",
            GeneratedAtUtc = report.GeneratedAtUtc,
            Headers =
            [
                "Product ID",
                "Product",
                "Application count",
                "Total quantity used",
                "First application UTC",
                "Last application UTC"
            ],
            Rows = report.Items
                .Select(item => (IReadOnlyList<string?>)
                [
                    item.ProductId.ToString(),
                    item.ProductName,
                    item.ApplicationCount.ToString(
                        CultureInfo.InvariantCulture),
                    FormatDecimal(item.TotalQuantityUsed),
                    item.FirstApplicationDateUtc?.ToString("O"),
                    item.LastApplicationDateUtc?.ToString("O")
                ])
                .ToList()
        };

        return ExportDocument(
            document,
            format,
            $"product-usage-{farmId:N}");
    }

    // GET: api/farms/{farmId}/reports/worker-activity/export/{format}
    [HttpGet("worker-activity/export/{format:alpha}")]
    [FarmPermission(PermissionCodes.ReportsView)]
    [FarmPermission(PermissionCodes.ReportsExport)]
    public async Task<IActionResult> ExportWorkerActivityDocument(
        Guid farmId,
        string format,
        [FromQuery] ApplicationReportQueryRequest request,
        CancellationToken cancellationToken)
    {
        var reportResult = await GetWorkerActivity(
            farmId,
            request,
            cancellationToken);

        if (reportResult is not OkObjectResult ok ||
            ok.Value is not WorkerActivityReportResponse report)
        {
            return reportResult;
        }

        if (report.Items.Count > MaximumExportRows)
            return ExportLimitExceeded();

        var document = new TabularExportDocument
        {
            Title = "Worker activity report",
            GeneratedAtUtc = report.GeneratedAtUtc,
            Headers =
            [
                "Worker ID",
                "Worker",
                "Worker code",
                "Active",
                "Application count",
                "Completed count",
                "Total area hectares",
                "Last application UTC"
            ],
            Rows = report.Items
                .Select(item => (IReadOnlyList<string?>)
                [
                    item.WorkerId.ToString(),
                    item.WorkerName,
                    item.WorkerCode,
                    item.IsActive ? "Yes" : "No",
                    item.ApplicationCount.ToString(
                        CultureInfo.InvariantCulture),
                    item.CompletedApplicationCount.ToString(
                        CultureInfo.InvariantCulture),
                    FormatDecimal(
                        item.TotalAreaTreatedHectares),
                    item.LastApplicationDateUtc?.ToString("O")
                ])
                .ToList()
        };

        return ExportDocument(
            document,
            format,
            $"worker-activity-{farmId:N}");
    }

    private async Task<TabularExportDocument?> BuildApplicationDocument(
        Guid farmId,
        ApplicationReportQueryRequest request,
        CancellationToken cancellationToken)
    {
        var items = await ProjectApplications(
                ApplyApplicationFilters(farmId, request))
            .OrderByDescending(item =>
                item.ApplicationDateUtc)
            .Take(MaximumExportRows + 1)
            .ToListAsync(cancellationToken);

        if (items.Count > MaximumExportRows)
            return null;

        return new TabularExportDocument
        {
            Title = "Pesticide application report",
            GeneratedAtUtc = _timeProvider.GetUtcNow(),
            Headers =
            [
                "Application ID",
                "Application date UTC",
                "Field",
                "Crop",
                "Worker",
                "Target pest",
                "Method",
                "Area treated hectares",
                "Status",
                "Product count"
            ],
            Rows = items
                .Select(item => (IReadOnlyList<string?>)
                [
                    item.Id.ToString(),
                    item.ApplicationDateUtc.ToString("O"),
                    item.FieldName,
                    item.CropName,
                    item.WorkerName,
                    item.TargetPest,
                    item.ApplicationMethod,
                    FormatDecimal(
                        item.AreaTreatedHectares),
                    item.Status,
                    item.ProductCount.ToString(
                        CultureInfo.InvariantCulture)
                ])
                .ToList()
        };
    }

    private async Task<TabularExportDocument?> BuildInventoryDocument(
        Guid farmId,
        InventoryReportQueryRequest request,
        CancellationToken cancellationToken)
    {
        var items = await ProjectInventory(
                ApplyInventoryFilters(farmId, request))
            .OrderByDescending(item => item.CreatedAtUtc)
            .Take(MaximumExportRows + 1)
            .ToListAsync(cancellationToken);

        if (items.Count > MaximumExportRows)
            return null;

        return new TabularExportDocument
        {
            Title = "Inventory transaction report",
            GeneratedAtUtc = _timeProvider.GetUtcNow(),
            Headers =
            [
                "Transaction ID",
                "Created at UTC",
                "Product",
                "Transaction type",
                "Quantity",
                "Batch",
                "Expiry date",
                "Reference",
                "Reason"
            ],
            Rows = items
                .Select(item => (IReadOnlyList<string?>)
                [
                    item.Id.ToString(),
                    item.CreatedAtUtc.ToString("O"),
                    item.ProductName,
                    item.TransactionType,
                    FormatDecimal(item.Quantity),
                    item.BatchNumber,
                    item.ExpiryDate?.ToString("yyyy-MM-dd"),
                    item.Reference,
                    item.Reason
                ])
                .ToList()
        };
    }

    private IQueryable<PesticideApplication> ApplyApplicationFilters(
        Guid farmId,
        ApplicationReportQueryRequest request)
    {
        var query = _db.PesticideApplications
            .AsNoTracking()
            .Where(application =>
                application.FarmId == farmId);

        if (request.FromUtc.HasValue)
        {
            var fromUtc = request.FromUtc.Value.UtcDateTime;
            query = query.Where(application =>
                application.ApplicationDateUtc >= fromUtc);
        }

        if (request.ToUtc.HasValue)
        {
            var toUtc = request.ToUtc.Value.UtcDateTime;
            query = query.Where(application =>
                application.ApplicationDateUtc <= toUtc);
        }

        if (request.FieldId.HasValue)
        {
            query = query.Where(application =>
                application.FieldId == request.FieldId.Value);
        }

        if (request.WorkerId.HasValue)
        {
            query = query.Where(application =>
                application.WorkerId == request.WorkerId.Value);
        }

        if (request.ProductId.HasValue)
        {
            query = query.Where(application =>
                application.Products.Any(product =>
                    product.ProductId == request.ProductId.Value));
        }

        if (request.Status.HasValue)
        {
            query = query.Where(application =>
                application.Status == request.Status.Value);
        }

        return query;
    }

    private IQueryable<InventoryTransaction> ApplyInventoryFilters(
        Guid farmId,
        InventoryReportQueryRequest request)
    {
        var query = _db.InventoryTransactions
            .AsNoTracking()
            .Where(transaction =>
                transaction.FarmId == farmId);

        if (request.FromUtc.HasValue)
        {
            var fromUtc = request.FromUtc.Value.UtcDateTime;
            query = query.Where(transaction =>
                transaction.CreatedAtUtc >= fromUtc);
        }

        if (request.ToUtc.HasValue)
        {
            var toUtc = request.ToUtc.Value.UtcDateTime;
            query = query.Where(transaction =>
                transaction.CreatedAtUtc <= toUtc);
        }

        if (request.ProductId.HasValue)
        {
            query = query.Where(transaction =>
                transaction.ProductId == request.ProductId.Value);
        }

        if (request.TransactionType.HasValue)
        {
            query = query.Where(transaction =>
                transaction.TransactionType ==
                    request.TransactionType.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.BatchNumber))
        {
            var batchNumber = request.BatchNumber.Trim();
            query = query.Where(transaction =>
                transaction.BatchNumber == batchNumber);
        }

        return query;
    }

    private static IQueryable<ApplicationReportItem> ProjectApplications(
        IQueryable<PesticideApplication> query)
    {
        return query.Select(application =>
            new ApplicationReportItem
            {
                Id = application.Id,
                ApplicationDateUtc = new DateTimeOffset(
                    DateTime.SpecifyKind(
                        application.ApplicationDateUtc,
                        DateTimeKind.Utc)),
                FieldName = application.Field.Name,
                CropName = application.FieldCrop.Crop.Name,
                WorkerName = application.Worker.FullName,
                TargetPest = application.TargetPest,
                ApplicationMethod =
                    application.ApplicationMethod,
                AreaTreatedHectares =
                    application.AreaTreatedHectares,
                Status =
                    application.Status ==
                        PesticideApplicationStatus.Draft
                        ? "Draft"
                        : "Completed",
                ProductCount = application.Products.Count
            });
    }

    private static IQueryable<InventoryReportItem> ProjectInventory(
        IQueryable<InventoryTransaction> query)
    {
        return query.Select(transaction =>
            new InventoryReportItem
            {
                Id = transaction.Id,
                CreatedAtUtc = new DateTimeOffset(
                    DateTime.SpecifyKind(
                        transaction.CreatedAtUtc,
                        DateTimeKind.Utc)),
                ProductName = transaction.Product.Name,
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
                BatchNumber = transaction.BatchNumber,
                ExpiryDate = transaction.ExpiryDate,
                Reference = transaction.Reference,
                Reason = transaction.Reason
            });
    }

    private Task<bool> FarmExists(
        Guid farmId,
        CancellationToken cancellationToken)
    {
        return _db.Farms
            .AsNoTracking()
            .AnyAsync(
                farm =>
                    farm.Id == farmId &&
                    farm.IsActive,
                cancellationToken);
    }

    private static string? ValidateDateRange(
        DateTimeOffset? fromUtc,
        DateTimeOffset? toUtc)
    {
        return fromUtc.HasValue &&
            toUtc.HasValue &&
            fromUtc.Value > toUtc.Value
                ? "FromUtc must be earlier than or equal to ToUtc."
                : null;
    }

    private static int CalculateTotalPages(
        int totalCount,
        int pageSize)
    {
        return (int)Math.Ceiling(
            totalCount / (double)pageSize);
    }

    private static int GetCount<T>(
        IReadOnlyDictionary<T, int> counts,
        T key)
        where T : notnull
    {
        return counts.TryGetValue(key, out var count)
            ? count
            : 0;
    }

    private static string FormatDecimal(decimal value)
    {
        return value.ToString(
            "0.###",
            CultureInfo.InvariantCulture);
    }

    private static DateTimeOffset ToUtcOffset(DateTime value)
    {
        return new DateTimeOffset(
            DateTime.SpecifyKind(
                value,
                DateTimeKind.Utc));
    }

    private FileContentResult CsvFile(
        byte[] content,
        string fileName)
    {
        return File(
            content,
            "text/csv; charset=utf-8",
            fileName);
    }

    private IActionResult ExportDocument(
        TabularExportDocument document,
        string format,
        string baseFileName)
    {
        if (string.Equals(
                format,
                "xlsx",
                StringComparison.OrdinalIgnoreCase))
        {
            return File(
                XlsxExportWriter.Write(document),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"{baseFileName}.xlsx");
        }

        if (string.Equals(
                format,
                "pdf",
                StringComparison.OrdinalIgnoreCase))
        {
            return File(
                PdfExportWriter.Write(document),
                "application/pdf",
                $"{baseFileName}.pdf");
        }

        return BadRequest(new
        {
            message = "Supported document formats are xlsx and pdf."
        });
    }

    private ObjectResult ExportLimitExceeded()
    {
        return StatusCode(
            StatusCodes.Status413PayloadTooLarge,
            new
            {
                message =
                    $"The export exceeds {MaximumExportRows} rows. Narrow the report filters and try again."
            });
    }
}

namespace PestiTrackBackend.DTOs.Reports;

public abstract class PagedReportResponse<T>
{
    public DateTimeOffset GeneratedAtUtc { get; set; }

    public int Page { get; set; }

    public int PageSize { get; set; }

    public int TotalCount { get; set; }

    public int TotalPages { get; set; }

    public List<T> Items { get; set; } = [];
}

public sealed class ApplicationReportResponse
    : PagedReportResponse<ApplicationReportItem>
{
    public int DraftCount { get; set; }

    public int CompletedCount { get; set; }

    public decimal TotalAreaTreatedHectares { get; set; }
}

public sealed class ApplicationReportItem
{
    public Guid Id { get; set; }

    public DateTimeOffset ApplicationDateUtc { get; set; }

    public string FieldName { get; set; } = string.Empty;

    public string CropName { get; set; } = string.Empty;

    public string WorkerName { get; set; } = string.Empty;

    public string TargetPest { get; set; } = string.Empty;

    public string ApplicationMethod { get; set; } = string.Empty;

    public decimal AreaTreatedHectares { get; set; }

    public string Status { get; set; } = string.Empty;

    public int ProductCount { get; set; }
}

public sealed class InventoryReportResponse
    : PagedReportResponse<InventoryReportItem>
{
    public int StockInCount { get; set; }

    public int StockOutCount { get; set; }

    public int AdjustmentInCount { get; set; }

    public int AdjustmentOutCount { get; set; }
}

public sealed class InventoryReportItem
{
    public Guid Id { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public string TransactionType { get; set; } = string.Empty;

    public decimal Quantity { get; set; }

    public string? BatchNumber { get; set; }

    public DateOnly? ExpiryDate { get; set; }

    public string? Reference { get; set; }

    public string? Reason { get; set; }
}

public sealed class ProductUsageReportResponse
{
    public DateTimeOffset GeneratedAtUtc { get; set; }

    public int TotalProducts { get; set; }

    public List<ProductUsageReportItem> Items { get; set; } = [];
}

public sealed class ProductUsageReportItem
{
    public Guid ProductId { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public int ApplicationCount { get; set; }

    public decimal TotalQuantityUsed { get; set; }

    public DateTimeOffset? FirstApplicationDateUtc { get; set; }

    public DateTimeOffset? LastApplicationDateUtc { get; set; }
}

public sealed class WorkerActivityReportResponse
{
    public DateTimeOffset GeneratedAtUtc { get; set; }

    public int TotalWorkers { get; set; }

    public List<WorkerActivityReportItem> Items { get; set; } = [];
}

public sealed class WorkerActivityReportItem
{
    public Guid WorkerId { get; set; }

    public string WorkerName { get; set; } = string.Empty;

    public string WorkerCode { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    public int ApplicationCount { get; set; }

    public int CompletedApplicationCount { get; set; }

    public decimal TotalAreaTreatedHectares { get; set; }

    public DateTimeOffset? LastApplicationDateUtc { get; set; }
}

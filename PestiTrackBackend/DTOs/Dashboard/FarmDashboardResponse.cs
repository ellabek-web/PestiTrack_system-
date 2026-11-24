namespace PestiTrackBackend.DTOs.Dashboard;

public sealed class SystemDashboardResponse
{
    public DateTimeOffset GeneratedAtUtc { get; set; }

    public int TotalFarms { get; set; }

    public int ActiveFarms { get; set; }

    public int TotalFields { get; set; }

    public int ActiveFields { get; set; }

    public int ActiveCropAssignments { get; set; }

    public int ActiveWorkers { get; set; }

    public int ActiveApplicators { get; set; }

    public int InventoryProductCount { get; set; }

    public int DraftApplications { get; set; }

    public int CompletedApplications { get; set; }

    public int CompletedApplicationsWithin30Days { get; set; }
}

public sealed class FarmDashboardResponse
{
    public Guid FarmId { get; set; }

    public string FarmName { get; set; } = string.Empty;

    public string FarmCode { get; set; } = string.Empty;

    public string? Location { get; set; }

    public DateTimeOffset GeneratedAtUtc { get; set; }

    public List<string> VisibleSections { get; set; } = [];

    public FieldDashboardSummary? Fields { get; set; }

    public CropDashboardSummary? Crops { get; set; }

    public WorkerDashboardSummary? Workers { get; set; }

    public InventoryDashboardSummary? Inventory { get; set; }

    public ApplicationDashboardSummary? Applications { get; set; }

    public List<RecentApplicationDashboardItem> RecentApplications { get; set; } = [];

    public List<RecentInventoryDashboardItem> RecentInventoryTransactions { get; set; } = [];
}

public sealed class FieldDashboardSummary
{
    public int Total { get; set; }

    public int Active { get; set; }

    public decimal TotalAreaHectares { get; set; }
}

public sealed class CropDashboardSummary
{
    public int TotalAssignments { get; set; }

    public int Planned { get; set; }

    public int Planted { get; set; }

    public int Harvested { get; set; }

    public int Cancelled { get; set; }
}

public sealed class WorkerDashboardSummary
{
    public int Total { get; set; }

    public int Active { get; set; }

    public int ActiveApplicators { get; set; }

    public int ExpiredCertifications { get; set; }

    public int CertificationsExpiringWithin30Days { get; set; }
}

public sealed class InventoryDashboardSummary
{
    public int StockLineCount { get; set; }

    public int ProductCount { get; set; }

    public int BatchCount { get; set; }

    public int ExpiredBatchCount { get; set; }

    public int BatchesExpiringWithin30Days { get; set; }
}

public sealed class ApplicationDashboardSummary
{
    public int Total { get; set; }

    public int Draft { get; set; }

    public int Completed { get; set; }

    public int CompletedWithin30Days { get; set; }

    public decimal AreaTreatedWithin30DaysHectares { get; set; }
}

public sealed class RecentApplicationDashboardItem
{
    public Guid Id { get; set; }

    public DateTimeOffset ApplicationDateUtc { get; set; }

    public string FieldName { get; set; } = string.Empty;

    public string CropName { get; set; } = string.Empty;

    public string WorkerName { get; set; } = string.Empty;

    public string TargetPest { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;
}

public sealed class RecentInventoryDashboardItem
{
    public Guid Id { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public string TransactionType { get; set; } = string.Empty;

    public decimal Quantity { get; set; }

    public string? BatchNumber { get; set; }

    public string? Reference { get; set; }
}

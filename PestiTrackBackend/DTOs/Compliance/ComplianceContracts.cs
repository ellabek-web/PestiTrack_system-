using System.ComponentModel.DataAnnotations;
using PestiTrackBackend.Models.Compliance;

namespace PestiTrackBackend.DTOs.Compliance;

public sealed class ComplianceRecordQueryRequest
{
    public ComplianceRecordStatus? Status { get; set; }

    public ComplianceRecordType? RecordType { get; set; }

    public ComplianceSeverity? Severity { get; set; }

    [StringLength(200)]
    public string? Search { get; set; }

    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;

    [Range(1, 100)]
    public int PageSize { get; set; } = 20;
}

public sealed class CreateComplianceRecordRequest
{
    [Required]
    public ComplianceRecordType RecordType { get; set; }

    [Required]
    [StringLength(200, MinimumLength = 3)]
    public string Title { get; set; } = string.Empty;

    public ComplianceSeverity Severity { get; set; }
        = ComplianceSeverity.Medium;

    public ComplianceRecordStatus Status { get; set; }
        = ComplianceRecordStatus.Open;

    public DateTime CheckedAtUtc { get; set; }

    public DateTime? DueDateUtc { get; set; }

    public Guid? FieldId { get; set; }

    public Guid? PesticideApplicationId { get; set; }

    public Guid? WorkerId { get; set; }

    [StringLength(150)]
    public string? InspectorName { get; set; }

    [StringLength(2000)]
    public string? Findings { get; set; }

    [StringLength(2000)]
    public string? CorrectiveAction { get; set; }
}

public sealed class UpdateComplianceRecordRequest
{
    [Required]
    public ComplianceRecordType RecordType { get; set; }

    [Required]
    [StringLength(200, MinimumLength = 3)]
    public string Title { get; set; } = string.Empty;

    public ComplianceSeverity Severity { get; set; }
        = ComplianceSeverity.Medium;

    public ComplianceRecordStatus Status { get; set; }
        = ComplianceRecordStatus.Open;

    public DateTime CheckedAtUtc { get; set; }

    public DateTime? DueDateUtc { get; set; }

    public Guid? FieldId { get; set; }

    public Guid? PesticideApplicationId { get; set; }

    public Guid? WorkerId { get; set; }

    [StringLength(150)]
    public string? InspectorName { get; set; }

    [StringLength(2000)]
    public string? Findings { get; set; }

    [StringLength(2000)]
    public string? CorrectiveAction { get; set; }
}

public sealed class UpdateComplianceRecordStatusRequest
{
    [Required]
    public ComplianceRecordStatus Status { get; set; }

    [StringLength(2000)]
    public string? CorrectiveAction { get; set; }
}

public sealed class ComplianceRecordResponse
{
    public Guid Id { get; set; }

    public Guid FarmId { get; set; }

    public Guid? FieldId { get; set; }

    public string? FieldName { get; set; }

    public Guid? PesticideApplicationId { get; set; }

    public Guid? WorkerId { get; set; }

    public string? WorkerName { get; set; }

    public string RecordType { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public string Severity { get; set; } = string.Empty;

    public DateTimeOffset CheckedAtUtc { get; set; }

    public DateTimeOffset? DueDateUtc { get; set; }

    public DateTimeOffset? ResolvedAtUtc { get; set; }

    public string? InspectorName { get; set; }

    public string? Findings { get; set; }

    public string? CorrectiveAction { get; set; }

    public Guid CreatedByUserId { get; set; }

    public string CreatedByDisplayName { get; set; } = string.Empty;

    public DateTimeOffset CreatedAtUtc { get; set; }
}

public sealed class ComplianceRecordListResponse
{
    public int Page { get; set; }

    public int PageSize { get; set; }

    public int TotalCount { get; set; }

    public int TotalPages { get; set; }

    public List<ComplianceRecordResponse> Items { get; set; } = [];
}

public sealed class ComplianceOverviewResponse
{
    public DateTimeOffset GeneratedAtUtc { get; set; }

    public int OpenRecords { get; set; }

    public int NonCompliantRecords { get; set; }

    public int ExpiredCertifications { get; set; }

    public int ExpiringCertificationsWithin30Days { get; set; }

    public int ActiveReEntryHolds { get; set; }

    public int ActivePreHarvestHolds { get; set; }

    public int ExpiringProductRegistrations { get; set; }

    public List<WorkerCertificationStatusItem> Certifications { get; set; } = [];

    public List<ReEntryHoldItem> ReEntryHolds { get; set; } = [];

    public List<PreHarvestHoldItem> PreHarvestHolds { get; set; } = [];

    public List<ProductRegistrationAlertItem> ProductRegistrations { get; set; } = [];
}

public sealed class WorkerCertificationStatusItem
{
    public Guid WorkerId { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string WorkerCode { get; set; } = string.Empty;

    public string? CertificationNumber { get; set; }

    public DateOnly? CertificationExpiryDate { get; set; }

    public string Status { get; set; } = string.Empty;

    public int? DaysRemaining { get; set; }
}

public sealed class ReEntryHoldItem
{
    public Guid ApplicationId { get; set; }

    public Guid FieldId { get; set; }

    public string FieldName { get; set; } = string.Empty;

    public string CropName { get; set; } = string.Empty;

    public DateTimeOffset ApplicationDateUtc { get; set; }

    public int ReEntryIntervalHours { get; set; }

    public DateTimeOffset ReEntryAllowedAtUtc { get; set; }

    public List<string> Products { get; set; } = [];
}

public sealed class PreHarvestHoldItem
{
    public Guid ApplicationId { get; set; }

    public Guid FieldId { get; set; }

    public string FieldName { get; set; } = string.Empty;

    public string CropName { get; set; } = string.Empty;

    public DateOnly ApplicationDate { get; set; }

    public int PreHarvestIntervalDays { get; set; }

    public DateOnly EarliestSafeHarvestDate { get; set; }

    public DateOnly? ExpectedHarvestDate { get; set; }

    public bool ConflictsWithExpectedHarvest { get; set; }

    public List<string> Products { get; set; } = [];
}

public sealed class ProductRegistrationAlertItem
{
    public Guid ProductId { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public string? RegistrationNumber { get; set; }

    public DateOnly RegistrationExpiryDate { get; set; }

    public string Status { get; set; } = string.Empty;

    public int DaysRemaining { get; set; }
}

public sealed class ApplicationSafetyCheckResponse
{
    public Guid ApplicationId { get; set; }

    public bool IsCompliant { get; set; }

    public int ViolationCount { get; set; }

    public int WarningCount { get; set; }

    public DateTimeOffset? ReEntryAllowedAtUtc { get; set; }

    public DateOnly? EarliestSafeHarvestDate { get; set; }

    public List<SafetyCheckItem> Checks { get; set; } = [];
}

public sealed class SafetyCheckItem
{
    public string Code { get; set; } = string.Empty;

    public string Severity { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;
}

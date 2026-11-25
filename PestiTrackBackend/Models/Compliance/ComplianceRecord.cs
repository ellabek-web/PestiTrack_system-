using PestiTrackBackend.Models.Applications;
using PestiTrackBackend.Models.Farms;
using PestiTrackBackend.Models.Identity;
using PestiTrackBackend.Models.Workers;

namespace PestiTrackBackend.Models.Compliance;

public enum ComplianceRecordType
{
    Inspection = 1,
    Incident = 2,
    AuditFinding = 3,
    CertificationReview = 4,
    RegulatoryCheck = 5
}

public enum ComplianceRecordStatus
{
    Open = 1,
    InReview = 2,
    Resolved = 3,
    NonCompliant = 4
}

public enum ComplianceSeverity
{
    Low = 1,
    Medium = 2,
    High = 3,
    Critical = 4
}

public sealed class ComplianceRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid FarmId { get; set; }

    public Guid? FieldId { get; set; }

    public Guid? PesticideApplicationId { get; set; }

    public Guid? WorkerId { get; set; }

    public ComplianceRecordType RecordType { get; set; }

    public string Title { get; set; } = string.Empty;

    public ComplianceRecordStatus Status { get; set; }
        = ComplianceRecordStatus.Open;

    public ComplianceSeverity Severity { get; set; }
        = ComplianceSeverity.Medium;

    public DateTime CheckedAtUtc { get; set; }

    public DateTime? DueDateUtc { get; set; }

    public DateTime? ResolvedAtUtc { get; set; }

    public string? InspectorName { get; set; }

    public string? Findings { get; set; }

    public string? CorrectiveAction { get; set; }

    public Guid CreatedByUserId { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public Farm Farm { get; set; } = null!;

    public Field? Field { get; set; }

    public PesticideApplication? PesticideApplication { get; set; }

    public Worker? Worker { get; set; }

    public ApplicationUser CreatedByUser { get; set; } = null!;
}

using PestiTrackBackend.Models.Farms;

namespace PestiTrackBackend.Models.Notifications;

public enum NotificationCategory
{
    LowStock = 0,
    ExpiringBatch = 1,
    ExpiredBatch = 2,
    ExpiringCertification = 3,
    ExpiredCertification = 4,
    ProductRegistration = 5,
    ReEntryHold = 6,
    PreHarvestConflict = 7,
    ComplianceAlert = 8
}

public enum NotificationSeverity
{
    Info = 0,
    Warning = 1,
    Critical = 2
}

public sealed class FarmNotification
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid FarmId { get; set; }

    public string DeduplicationKey { get; set; } = string.Empty;

    public NotificationCategory Category { get; set; }

    public NotificationSeverity Severity { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public string? ActionPath { get; set; }

    public bool IsRead { get; set; }

    public DateTime? ReadAtUtc { get; set; }

    public bool IsResolved { get; set; }

    public DateTime? ResolvedAtUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime LastEvaluatedAtUtc { get; set; } = DateTime.UtcNow;

    public Farm Farm { get; set; } = null!;
}

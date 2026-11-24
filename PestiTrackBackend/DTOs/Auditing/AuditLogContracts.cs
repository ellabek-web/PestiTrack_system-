using System.ComponentModel.DataAnnotations;

namespace PestiTrackBackend.DTOs.Auditing;

public sealed class AuditLogQueryRequest : IValidatableObject
{
    public DateTime? FromUtc { get; set; }

    public DateTime? ToUtc { get; set; }

    public Guid? ActorUserId { get; set; }

    [MaxLength(50)]
    public string? Action { get; set; }

    [MaxLength(150)]
    public string? EntityType { get; set; }

    [MaxLength(200)]
    public string? Search { get; set; }

    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;

    [Range(1, 100)]
    public int PageSize { get; set; } = 25;

    public IEnumerable<ValidationResult> Validate(
        ValidationContext validationContext)
    {
        if (FromUtc.HasValue &&
            ToUtc.HasValue &&
            FromUtc.Value > ToUtc.Value)
        {
            yield return new ValidationResult(
                "FromUtc must be earlier than or equal to ToUtc.",
                [nameof(FromUtc), nameof(ToUtc)]);
        }
    }
}

public sealed class AuditValueChange
{
    public object? OldValue { get; set; }

    public object? NewValue { get; set; }
}

public sealed class AuditLogResponse
{
    public Guid Id { get; set; }

    public DateTime OccurredAtUtc { get; set; }

    public Guid? ActorUserId { get; set; }

    public string? ActorDisplayName { get; set; }

    public string? ActorEmail { get; set; }

    public Guid? FarmId { get; set; }

    public string Action { get; set; } = string.Empty;

    public string EntityType { get; set; } = string.Empty;

    public string EntityId { get; set; } = string.Empty;

    public Dictionary<string, AuditValueChange> Changes { get; set; } = [];

    public string? HttpMethod { get; set; }

    public string? RequestPath { get; set; }

    public string? IpAddress { get; set; }

    public string? UserAgent { get; set; }
}

public sealed class AuditLogListResponse
{
    public IReadOnlyList<AuditLogResponse> Items { get; set; } = [];

    public int Page { get; set; }

    public int PageSize { get; set; }

    public int TotalCount { get; set; }

    public int TotalPages { get; set; }
}

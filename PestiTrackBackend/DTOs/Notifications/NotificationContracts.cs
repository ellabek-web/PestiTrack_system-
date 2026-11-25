using System.ComponentModel.DataAnnotations;
using PestiTrackBackend.Models.Notifications;

namespace PestiTrackBackend.DTOs.Notifications;

public sealed class NotificationQueryRequest
{
    public NotificationCategory? Category { get; set; }

    public NotificationSeverity? Severity { get; set; }

    public bool UnreadOnly { get; set; }

    public bool IncludeResolved { get; set; }

    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;

    [Range(1, 100)]
    public int PageSize { get; set; } = 25;
}

public sealed class MarkNotificationReadRequest
{
    public bool IsRead { get; set; } = true;
}

public sealed class NotificationResponse
{
    public Guid Id { get; set; }

    public Guid FarmId { get; set; }

    public string Category { get; set; } = string.Empty;

    public string Severity { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public string? ActionPath { get; set; }

    public bool IsRead { get; set; }

    public DateTimeOffset? ReadAtUtc { get; set; }

    public bool IsResolved { get; set; }

    public DateTimeOffset? ResolvedAtUtc { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset LastEvaluatedAtUtc { get; set; }
}

public sealed class NotificationListResponse
{
    public int Page { get; set; }

    public int PageSize { get; set; }

    public int TotalCount { get; set; }

    public int TotalPages { get; set; }

    public int UnreadCount { get; set; }

    public int CriticalCount { get; set; }

    public IReadOnlyList<NotificationResponse> Items { get; set; } = [];
}

public sealed class NotificationSummaryResponse
{
    public int UnreadCount { get; set; }

    public int CriticalCount { get; set; }
}

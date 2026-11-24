using PestiTrackBackend.Models.Identity;

namespace PestiTrackBackend.Models.Auditing;

public sealed class AuditLog
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public DateTime OccurredAtUtc { get; set; }

    public Guid? ActorUserId { get; set; }

    public string? ActorDisplayName { get; set; }

    public string? ActorEmail { get; set; }

    public Guid? FarmId { get; set; }

    public string Action { get; set; } = string.Empty;

    public string EntityType { get; set; } = string.Empty;

    public string EntityId { get; set; } = string.Empty;

    public string ChangesJson { get; set; } = "{}";

    public string? HttpMethod { get; set; }

    public string? RequestPath { get; set; }

    public string? IpAddress { get; set; }

    public string? UserAgent { get; set; }

    public ApplicationUser? ActorUser { get; set; }
}

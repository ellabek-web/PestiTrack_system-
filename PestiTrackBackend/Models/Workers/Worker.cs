using PestiTrackBackend.Models.Farms;

namespace PestiTrackBackend.Models.Workers;

public sealed class Worker
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid FarmId { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string WorkerCode { get; set; } = string.Empty;

    public string? PhoneNumber { get; set; }

    public string? Email { get; set; }

    public bool IsApplicator { get; set; } = false;

    public string? CertificationNumber { get; set; }

    public DateTime? CertificationExpiryDate { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public Farm Farm { get; set; } = null!;
}

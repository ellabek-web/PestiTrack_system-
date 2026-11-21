namespace PestiTrackBackend.DTOs.Workers;

public sealed class WorkerResponse
{
    public Guid Id { get; set; }
    public Guid FarmId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string WorkerCode { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public bool IsApplicator { get; set; }
    public string? CertificationNumber { get; set; }
    public DateTime? CertificationExpiryDate { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
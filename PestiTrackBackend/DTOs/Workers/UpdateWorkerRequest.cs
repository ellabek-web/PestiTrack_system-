using System.ComponentModel.DataAnnotations;

namespace PestiTrackBackend.DTOs.Workers;

public sealed class UpdateWorkerRequest
{
    [Required]
    [StringLength(200, MinimumLength = 2)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [StringLength(50, MinimumLength = 2)]
    [RegularExpression(@"^[A-Za-z0-9_-]+$")]
    public string WorkerCode { get; set; } = string.Empty;

    [StringLength(30)]
    public string? PhoneNumber { get; set; }

    [EmailAddress]
    [StringLength(256)]
    public string? Email { get; set; }

    public bool IsApplicator { get; set; }

    [StringLength(100)]
    public string? CertificationNumber { get; set; }

    public DateTime? CertificationExpiryDate { get; set; }
}
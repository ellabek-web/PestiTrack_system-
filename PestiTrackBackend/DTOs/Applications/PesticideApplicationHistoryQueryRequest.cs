using System.ComponentModel.DataAnnotations;
using PestiTrackBackend.Models.Applications;

namespace PestiTrackBackend.DTOs.Applications;

public sealed class PesticideApplicationHistoryQueryRequest
{
    public Guid? FieldId { get; set; }

    public Guid? FieldCropId { get; set; }

    public Guid? WorkerId { get; set; }

    public Guid? ProductId { get; set; }

    public PesticideApplicationStatus? Status { get; set; }

    public DateTimeOffset? FromUtc { get; set; }

    public DateTimeOffset? ToUtc { get; set; }

    [StringLength(200)]
    public string? Search { get; set; }

    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;

    [Range(1, 100)]
    public int PageSize { get; set; } = 20;
}

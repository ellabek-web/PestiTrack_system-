using System.ComponentModel.DataAnnotations;
using PestiTrackBackend.Models.Applications;
using PestiTrackBackend.Models.Inventory;

namespace PestiTrackBackend.DTOs.Reports;

public class ApplicationReportQueryRequest
{
    public DateTimeOffset? FromUtc { get; set; }

    public DateTimeOffset? ToUtc { get; set; }

    public Guid? FieldId { get; set; }

    public Guid? WorkerId { get; set; }

    public Guid? ProductId { get; set; }

    public PesticideApplicationStatus? Status { get; set; }

    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;

    [Range(1, 200)]
    public int PageSize { get; set; } = 50;
}

public sealed class InventoryReportQueryRequest
{
    public DateTimeOffset? FromUtc { get; set; }

    public DateTimeOffset? ToUtc { get; set; }

    public Guid? ProductId { get; set; }

    public InventoryTransactionType? TransactionType { get; set; }

    [StringLength(100)]
    public string? BatchNumber { get; set; }

    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;

    [Range(1, 200)]
    public int PageSize { get; set; } = 50;
}

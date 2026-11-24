namespace PestiTrackBackend.DTOs.Applications;

public sealed class PesticideApplicationHistoryResponse
{
    public int Page { get; set; }

    public int PageSize { get; set; }

    public int TotalCount { get; set; }

    public int TotalPages { get; set; }

    public List<PesticideApplicationHistoryItemResponse> Items { get; set; } = [];
}

public sealed class PesticideApplicationHistoryItemResponse
{
    public Guid Id { get; set; }

    public Guid FarmId { get; set; }

    public Guid FieldId { get; set; }

    public string FieldName { get; set; } = string.Empty;

    public Guid FieldCropId { get; set; }

    public string CropName { get; set; } = string.Empty;

    public Guid WorkerId { get; set; }

    public string WorkerName { get; set; } = string.Empty;

    public DateTimeOffset ApplicationDateUtc { get; set; }

    public string TargetPest { get; set; } = string.Empty;

    public string ApplicationMethod { get; set; } = string.Empty;

    public decimal AreaTreatedHectares { get; set; }

    public string Status { get; set; } = string.Empty;

    public int ProductCount { get; set; }

    public bool HasInventoryTraceability { get; set; }

    public Guid CreatedByUserId { get; set; }

    public string CreatedByDisplayName { get; set; } = string.Empty;

    public DateTimeOffset CreatedAtUtc { get; set; }
}

namespace PestiTrackBackend.DTOs.Applications;

public sealed class PesticideApplicationResponse
{
    public Guid Id { get; set; }

    public Guid FarmId { get; set; }

    public Guid FieldId { get; set; }

    public string FieldName { get; set; } = string.Empty;

    public Guid FieldCropId { get; set; }

    public string CropName { get; set; } = string.Empty;

    public Guid WorkerId { get; set; }

    public string WorkerName { get; set; } = string.Empty;

    public DateTime ApplicationDateUtc { get; set; }

    public string TargetPest { get; set; } = string.Empty;

    public string ApplicationMethod { get; set; } = string.Empty;

    public string? Equipment { get; set; }

    public decimal AreaTreatedHectares { get; set; }

    public Guid? WeatherRecordId { get; set; }

    public string? Notes { get; set; }

    public string Status { get; set; } = string.Empty;

    public Guid CreatedByUserId { get; set; }

    public string CreatedByDisplayName { get; set; } = string.Empty;

    public string? CreatedByEmail { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public ApplicationWeatherResponse? Weather { get; set; }

    public List<ApplicationProductResponse> Products { get; set; } = new();
}

public sealed class ApplicationProductResponse
{
    public Guid Id { get; set; }

    public Guid ProductId { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public string BatchNumber { get; set; } = string.Empty;

    public decimal QuantityUsed { get; set; }

    public decimal ApplicationRate { get; set; }

    public string RateUnit { get; set; } = string.Empty;

    public Guid? InventoryTransactionId { get; set; }

    public ApplicationInventoryTransactionResponse? InventoryTransaction { get; set; }
}

public sealed class ApplicationWeatherResponse
{
    public Guid Id { get; set; }

    public DateTimeOffset RecordedAtUtc { get; set; }

    public decimal? TemperatureCelsius { get; set; }

    public decimal? HumidityPercentage { get; set; }

    public decimal? RainfallMillimeters { get; set; }

    public decimal? WindSpeedMetersPerSecond { get; set; }

    public decimal? WindDirectionDegrees { get; set; }

    public string? Conditions { get; set; }

    public string Source { get; set; } = string.Empty;
}

public sealed class ApplicationInventoryTransactionResponse
{
    public Guid Id { get; set; }

    public string TransactionType { get; set; } = string.Empty;

    public decimal Quantity { get; set; }

    public string? BatchNumber { get; set; }

    public DateOnly? ExpiryDate { get; set; }

    public string? Reference { get; set; }

    public string? Reason { get; set; }

    public Guid CreatedByUserId { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
}
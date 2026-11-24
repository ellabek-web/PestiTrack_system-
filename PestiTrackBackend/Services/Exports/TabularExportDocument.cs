namespace PestiTrackBackend.Services.Exports;

public sealed class TabularExportDocument
{
    public string Title { get; init; } = string.Empty;

    public DateTimeOffset GeneratedAtUtc { get; init; }

    public IReadOnlyList<string> Headers { get; init; } = [];

    public IReadOnlyList<IReadOnlyList<string?>> Rows { get; init; } = [];
}

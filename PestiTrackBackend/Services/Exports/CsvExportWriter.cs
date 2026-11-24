using System.Text;

namespace PestiTrackBackend.Services.Exports;

public static class CsvExportWriter
{
    public static byte[] Write(
        IReadOnlyList<string> headers,
        IEnumerable<IReadOnlyList<string?>> rows)
    {
        var csv = new StringBuilder();
        AppendRow(csv, headers);

        foreach (var row in rows)
            AppendRow(csv, row);

        return new UTF8Encoding(
            encoderShouldEmitUTF8Identifier: true)
            .GetBytes(csv.ToString());
    }

    private static void AppendRow(
        StringBuilder csv,
        IReadOnlyList<string?> values)
    {
        for (var index = 0; index < values.Count; index++)
        {
            if (index > 0)
                csv.Append(',');

            var value = ProtectFormula(
                values[index] ?? string.Empty);
            var requiresQuotes =
                value.Contains(',') ||
                value.Contains('"') ||
                value.Contains('\r') ||
                value.Contains('\n');

            if (!requiresQuotes)
            {
                csv.Append(value);
                continue;
            }

            csv.Append('"');
            csv.Append(value.Replace("\"", "\"\""));
            csv.Append('"');
        }

        csv.Append("\r\n");
    }

    private static string ProtectFormula(string value)
    {
        if (value.Length == 0)
            return value;

        return value[0] is '=' or '+' or '-' or '@' or '\t' or '\r'
            ? $"'{value}"
            : value;
    }
}

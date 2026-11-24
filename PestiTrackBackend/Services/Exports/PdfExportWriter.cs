using System.Globalization;
using System.Text;

namespace PestiTrackBackend.Services.Exports;

public static class PdfExportWriter
{
    private const int RowsPerPage = 36;

    public static byte[] Write(TabularExportDocument document)
    {
        var pages = BuildPages(document);
        var fontObjectId = 3 + pages.Count * 2;
        var objects = new Dictionary<int, byte[]>
        {
            [1] = Ascii(
                "<< /Type /Catalog /Pages 2 0 R >>"),
            [2] = Ascii(
                $"<< /Type /Pages /Count {pages.Count} /Kids [{string.Join(
                    " ",
                    Enumerable.Range(0, pages.Count)
                        .Select(index =>
                            $"{3 + index * 2} 0 R"))}] >>"),
            [fontObjectId] = Ascii(
                "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica /Encoding /WinAnsiEncoding >>")
        };

        for (var pageIndex = 0;
             pageIndex < pages.Count;
             pageIndex++)
        {
            var pageObjectId = 3 + pageIndex * 2;
            var contentObjectId = pageObjectId + 1;
            var content = BuildPageContent(
                pages[pageIndex],
                pageIndex + 1,
                pages.Count);

            objects[pageObjectId] = Ascii(
                $"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 842 595] /Resources << /Font << /F1 {fontObjectId} 0 R >> >> /Contents {contentObjectId} 0 R >>");
            objects[contentObjectId] = Ascii(
                $"<< /Length {Encoding.ASCII.GetByteCount(content)} >>\nstream\n{content}\nendstream");
        }

        return WritePdf(
            objects,
            fontObjectId);
    }

    private static List<List<string>> BuildPages(
        TabularExportDocument document)
    {
        var header = FormatRow(document.Headers);
        var dataRows = document.Rows
            .Select(FormatRow)
            .ToList();
        var pages = new List<List<string>>();

        if (dataRows.Count == 0)
        {
            pages.Add(
                [
                    document.Title,
                    $"Generated at UTC: {document.GeneratedAtUtc:O}",
                    string.Empty,
                    header,
                    new string('-', Math.Min(150, header.Length)),
                    "No records matched the report filters."
                ]);
            return pages;
        }

        for (var offset = 0;
             offset < dataRows.Count;
             offset += RowsPerPage)
        {
            var lines = new List<string>
            {
                document.Title,
                $"Generated at UTC: {document.GeneratedAtUtc:O}",
                string.Empty,
                header,
                new string('-', Math.Min(150, header.Length))
            };
            lines.AddRange(
                dataRows.Skip(offset).Take(RowsPerPage));
            pages.Add(lines);
        }

        return pages;
    }

    private static string FormatRow(
        IReadOnlyList<string?> values)
    {
        if (values.Count == 0)
            return string.Empty;

        var cellLimit = Math.Max(
            8,
            145 / values.Count);

        return string.Join(
            " | ",
            values.Select(value =>
                Truncate(
                    Normalize(value),
                    cellLimit)));
    }

    private static string BuildPageContent(
        IReadOnlyList<string> lines,
        int pageNumber,
        int pageCount)
    {
        var content = new StringBuilder();
        content.AppendLine("BT");
        content.AppendLine("/F1 9 Tf");
        content.AppendLine("36 558 Td");
        content.AppendLine("12 TL");

        foreach (var line in lines)
        {
            content.Append('(');
            content.Append(EscapePdfText(line));
            content.AppendLine(") Tj");
            content.AppendLine("T*");
        }

        content.AppendLine("ET");
        content.AppendLine("BT");
        content.AppendLine("/F1 8 Tf");
        content.AppendLine("760 20 Td");
        content.Append('(');
        content.Append(
            EscapePdfText(
                $"Page {pageNumber} of {pageCount}"));
        content.AppendLine(") Tj");
        content.AppendLine("ET");
        return content.ToString();
    }

    private static byte[] WritePdf(
        IReadOnlyDictionary<int, byte[]> objects,
        int maximumObjectId)
    {
        using var output = new MemoryStream();
        WriteAscii(output, "%PDF-1.4\n");
        output.Write(
            [0x25, 0xE2, 0xE3, 0xCF, 0xD3, 0x0A]);

        var offsets = new long[maximumObjectId + 1];

        for (var objectId = 1;
             objectId <= maximumObjectId;
             objectId++)
        {
            offsets[objectId] = output.Position;
            WriteAscii(
                output,
                $"{objectId} 0 obj\n");
            output.Write(objects[objectId]);
            WriteAscii(output, "\nendobj\n");
        }

        var xrefOffset = output.Position;
        WriteAscii(
            output,
            $"xref\n0 {maximumObjectId + 1}\n");
        WriteAscii(
            output,
            "0000000000 65535 f \n");

        for (var objectId = 1;
             objectId <= maximumObjectId;
             objectId++)
        {
            WriteAscii(
                output,
                $"{offsets[objectId].ToString(
                    "D10",
                    CultureInfo.InvariantCulture)} 00000 n \n");
        }

        WriteAscii(
            output,
            $"trailer\n<< /Size {maximumObjectId + 1} /Root 1 0 R >>\nstartxref\n{xrefOffset}\n%%EOF");

        return output.ToArray();
    }

    private static string Normalize(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        var normalized = value
            .Replace('\r', ' ')
            .Replace('\n', ' ')
            .Replace('\t', ' ');

        return new string(
            normalized
                .Select(character =>
                    character is >= ' ' and <= '~'
                        ? character
                        : '?')
                .ToArray());
    }

    private static string Truncate(
        string value,
        int maximumLength)
    {
        if (value.Length <= maximumLength)
            return value;

        return maximumLength <= 3
            ? value[..maximumLength]
            : $"{value[..(maximumLength - 3)]}...";
    }

    private static string EscapePdfText(string value)
    {
        return value
            .Replace("\\", "\\\\")
            .Replace("(", "\\(")
            .Replace(")", "\\)");
    }

    private static byte[] Ascii(string value)
    {
        return Encoding.ASCII.GetBytes(value);
    }

    private static void WriteAscii(
        Stream stream,
        string value)
    {
        stream.Write(Ascii(value));
    }
}

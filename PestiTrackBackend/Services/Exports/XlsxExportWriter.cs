using System.IO.Compression;
using System.Text;
using System.Xml;

namespace PestiTrackBackend.Services.Exports;

public static class XlsxExportWriter
{
    private const string SpreadsheetNamespace =
        "http://schemas.openxmlformats.org/spreadsheetml/2006/main";

    public static byte[] Write(TabularExportDocument document)
    {
        using var output = new MemoryStream();

        using (var archive = new ZipArchive(
            output,
            ZipArchiveMode.Create,
            leaveOpen: true))
        {
            WriteTextEntry(
                archive,
                "[Content_Types].xml",
                ContentTypesXml);
            WriteTextEntry(
                archive,
                "_rels/.rels",
                RootRelationshipsXml);
            WriteTextEntry(
                archive,
                "xl/workbook.xml",
                WorkbookXml);
            WriteTextEntry(
                archive,
                "xl/_rels/workbook.xml.rels",
                WorkbookRelationshipsXml);
            WriteTextEntry(
                archive,
                "xl/styles.xml",
                StylesXml);
            WriteWorksheet(
                archive,
                document);
        }

        return output.ToArray();
    }

    private static void WriteWorksheet(
        ZipArchive archive,
        TabularExportDocument document)
    {
        var entry = archive.CreateEntry(
            "xl/worksheets/sheet1.xml",
            CompressionLevel.Fastest);

        using var stream = entry.Open();
        using var writer = XmlWriter.Create(
            stream,
            new XmlWriterSettings
            {
                Encoding = new UTF8Encoding(false),
                Indent = false,
                CloseOutput = false
            });

        writer.WriteStartDocument();
        writer.WriteStartElement(
            "worksheet",
            SpreadsheetNamespace);

        writer.WriteStartElement(
            "sheetViews",
            SpreadsheetNamespace);
        writer.WriteStartElement(
            "sheetView",
            SpreadsheetNamespace);
        writer.WriteAttributeString("workbookViewId", "0");
        writer.WriteStartElement(
            "pane",
            SpreadsheetNamespace);
        writer.WriteAttributeString("ySplit", "3");
        writer.WriteAttributeString("topLeftCell", "A4");
        writer.WriteAttributeString("activePane", "bottomLeft");
        writer.WriteAttributeString("state", "frozen");
        writer.WriteEndElement();
        writer.WriteEndElement();
        writer.WriteEndElement();

        WriteColumns(writer, document);

        writer.WriteStartElement(
            "sheetData",
            SpreadsheetNamespace);

        WriteRow(
            writer,
            1,
            [document.Title],
            styleIndex: 2);
        WriteRow(
            writer,
            2,
            [
                $"Generated at UTC: {document.GeneratedAtUtc:O}"
            ],
            styleIndex: 3);
        WriteRow(
            writer,
            3,
            document.Headers,
            styleIndex: 1);

        var rowIndex = 4;
        foreach (var row in document.Rows)
        {
            WriteRow(
                writer,
                rowIndex,
                row,
                styleIndex: 0);
            rowIndex++;
        }

        writer.WriteEndElement();

        if (document.Headers.Count > 0)
        {
            writer.WriteStartElement(
                "autoFilter",
                SpreadsheetNamespace);
            writer.WriteAttributeString(
                "ref",
                $"A3:{ColumnName(document.Headers.Count)}{Math.Max(3, rowIndex - 1)}");
            writer.WriteEndElement();
        }

        writer.WriteEndElement();
        writer.WriteEndDocument();
    }

    private static void WriteColumns(
        XmlWriter writer,
        TabularExportDocument document)
    {
        if (document.Headers.Count == 0)
            return;

        writer.WriteStartElement(
            "cols",
            SpreadsheetNamespace);

        for (var index = 0; index < document.Headers.Count; index++)
        {
            var maximumLength = document.Rows
                .Select(row =>
                    index < row.Count
                        ? row[index]?.Length ?? 0
                        : 0)
                .Prepend(document.Headers[index].Length)
                .Take(500)
                .Max();
            var width = Math.Clamp(
                maximumLength + 2,
                10,
                40);

            writer.WriteStartElement(
                "col",
                SpreadsheetNamespace);
            writer.WriteAttributeString(
                "min",
                (index + 1).ToString());
            writer.WriteAttributeString(
                "max",
                (index + 1).ToString());
            writer.WriteAttributeString(
                "width",
                width.ToString());
            writer.WriteAttributeString(
                "customWidth",
                "1");
            writer.WriteEndElement();
        }

        writer.WriteEndElement();
    }

    private static void WriteRow(
        XmlWriter writer,
        int rowIndex,
        IReadOnlyList<string?> values,
        int styleIndex)
    {
        writer.WriteStartElement(
            "row",
            SpreadsheetNamespace);
        writer.WriteAttributeString(
            "r",
            rowIndex.ToString());

        for (var columnIndex = 0;
             columnIndex < values.Count;
             columnIndex++)
        {
            writer.WriteStartElement(
                "c",
                SpreadsheetNamespace);
            writer.WriteAttributeString(
                "r",
                $"{ColumnName(columnIndex + 1)}{rowIndex}");
            writer.WriteAttributeString("t", "inlineStr");

            if (styleIndex > 0)
            {
                writer.WriteAttributeString(
                    "s",
                    styleIndex.ToString());
            }

            writer.WriteStartElement(
                "is",
                SpreadsheetNamespace);
            writer.WriteStartElement(
                "t",
                SpreadsheetNamespace);
            writer.WriteAttributeString(
                "xml",
                "space",
                null,
                "preserve");
            writer.WriteString(values[columnIndex] ?? string.Empty);
            writer.WriteEndElement();
            writer.WriteEndElement();
            writer.WriteEndElement();
        }

        writer.WriteEndElement();
    }

    private static string ColumnName(int columnNumber)
    {
        var name = string.Empty;

        while (columnNumber > 0)
        {
            columnNumber--;
            name =
                (char)('A' + columnNumber % 26) +
                name;
            columnNumber /= 26;
        }

        return name;
    }

    private static void WriteTextEntry(
        ZipArchive archive,
        string path,
        string content)
    {
        var entry = archive.CreateEntry(
            path,
            CompressionLevel.Fastest);

        using var stream = entry.Open();
        using var writer = new StreamWriter(
            stream,
            new UTF8Encoding(false));
        writer.Write(content);
    }

    private const string ContentTypesXml =
        """
        <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
        <Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">
          <Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/>
          <Default Extension="xml" ContentType="application/xml"/>
          <Override PartName="/xl/workbook.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml"/>
          <Override PartName="/xl/worksheets/sheet1.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/>
          <Override PartName="/xl/styles.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml"/>
        </Types>
        """;

    private const string RootRelationshipsXml =
        """
        <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
        <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
          <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="xl/workbook.xml"/>
        </Relationships>
        """;

    private const string WorkbookXml =
        """
        <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
        <workbook xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships">
          <sheets>
            <sheet name="Report" sheetId="1" r:id="rId1"/>
          </sheets>
        </workbook>
        """;

    private const string WorkbookRelationshipsXml =
        """
        <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
        <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
          <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet1.xml"/>
          <Relationship Id="rId2" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles" Target="styles.xml"/>
        </Relationships>
        """;

    private const string StylesXml =
        """
        <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
        <styleSheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main">
          <fonts count="3">
            <font><sz val="11"/><name val="Calibri"/></font>
            <font><b/><color rgb="FFFFFFFF"/><sz val="11"/><name val="Calibri"/></font>
            <font><b/><sz val="16"/><name val="Calibri"/></font>
          </fonts>
          <fills count="3">
            <fill><patternFill patternType="none"/></fill>
            <fill><patternFill patternType="gray125"/></fill>
            <fill><patternFill patternType="solid"><fgColor rgb="FF2E7D32"/><bgColor indexed="64"/></patternFill></fill>
          </fills>
          <borders count="1"><border><left/><right/><top/><bottom/><diagonal/></border></borders>
          <cellStyleXfs count="1"><xf numFmtId="0" fontId="0" fillId="0" borderId="0"/></cellStyleXfs>
          <cellXfs count="4">
            <xf numFmtId="0" fontId="0" fillId="0" borderId="0" xfId="0"/>
            <xf numFmtId="0" fontId="1" fillId="2" borderId="0" xfId="0" applyFont="1" applyFill="1"/>
            <xf numFmtId="0" fontId="2" fillId="0" borderId="0" xfId="0" applyFont="1"/>
            <xf numFmtId="0" fontId="0" fillId="0" borderId="0" xfId="0"/>
          </cellXfs>
          <cellStyles count="1"><cellStyle name="Normal" xfId="0" builtinId="0"/></cellStyles>
        </styleSheet>
        """;
}

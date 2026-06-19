using System.IO;
using System.IO.Compression;
using System.Security;
using System.Text;
using WarrantyReturns.Desktop.Models;

namespace WarrantyReturns.Desktop.Services;

public sealed class ExportService
{
    public void ExportDocx(IEnumerable<WarrantyCase> cases, string fileName)
    {
        using var archive = ZipFile.Open(fileName, ZipArchiveMode.Create);
        AddText(archive, "[Content_Types].xml", """
            <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
            <Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">
              <Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/>
              <Default Extension="xml" ContentType="application/xml"/>
              <Override PartName="/word/document.xml" ContentType="application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml"/>
            </Types>
            """);
        AddText(archive, "_rels/.rels", """
            <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
            <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
              <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="word/document.xml"/>
            </Relationships>
            """);

        var rows = new StringBuilder();
        foreach (var item in cases)
        {
            rows.Append("<w:tr>");
            foreach (var cell in new[]
                     {
                         item.Id, item.ClientName, item.Phone, item.ProductName, item.Reason,
                         item.Status, item.CheckSum.ToString("0.00"), item.ReceivedAt.ToString("dd.MM.yyyy"),
                         item.ManagerComment
                     })
            {
                rows.Append("<w:tc><w:p><w:r><w:t>");
                rows.Append(SecurityElement.Escape(cell));
                rows.Append("</w:t></w:r></w:p></w:tc>");
            }
            rows.Append("</w:tr>");
        }

        var document = $$"""
            <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
            <w:document xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main">
              <w:body>
                <w:p><w:r><w:t>Реестр возвратов и гарантийных случаев</w:t></w:r></w:p>
                <w:tbl>
                  {{rows}}
                </w:tbl>
              </w:body>
            </w:document>
            """;
        AddText(archive, "word/document.xml", document);
    }

    public void ExportXlsx(IEnumerable<WarrantyCase> cases, string fileName)
    {
        using var archive = ZipFile.Open(fileName, ZipArchiveMode.Create);
        AddText(archive, "[Content_Types].xml", """
            <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
            <Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">
              <Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/>
              <Default Extension="xml" ContentType="application/xml"/>
              <Override PartName="/xl/workbook.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml"/>
              <Override PartName="/xl/worksheets/sheet1.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/>
            </Types>
            """);
        AddText(archive, "_rels/.rels", """
            <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
            <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
              <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="xl/workbook.xml"/>
            </Relationships>
            """);
        AddText(archive, "xl/_rels/workbook.xml.rels", """
            <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
            <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
              <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet1.xml"/>
            </Relationships>
            """);
        AddText(archive, "xl/workbook.xml", """
            <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
            <workbook xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main"
                      xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships">
              <sheets><sheet name="Возвраты" sheetId="1" r:id="rId1"/></sheets>
            </workbook>
            """);

        var rows = new StringBuilder();
        var allRows = new List<string[]>
        {
            new[] { "ID", "Клиент", "Телефон", "Товар", "Причина", "Статус", "Сумма", "Дата" }
        };
        allRows.AddRange(cases.Select(x => new[]
        {
            x.Id, x.ClientName, x.Phone, x.ProductName, x.Reason, x.Status,
            x.CheckSum.ToString("0.00"), x.ReceivedAt.ToString("dd.MM.yyyy")
        }));

        for (var r = 0; r < allRows.Count; r++)
        {
            rows.Append($"<row r=\"{r + 1}\">");
            for (var c = 0; c < allRows[r].Length; c++)
            {
                var name = $"{(char)('A' + c)}{r + 1}";
                rows.Append($"<c r=\"{name}\" t=\"inlineStr\"><is><t>");
                rows.Append(SecurityElement.Escape(allRows[r][c]));
                rows.Append("</t></is></c>");
            }
            rows.Append("</row>");
        }

        AddText(archive, "xl/worksheets/sheet1.xml", $$"""
            <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
            <worksheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main">
              <sheetData>{{rows}}</sheetData>
            </worksheet>
            """);
    }

    private static void AddText(ZipArchive archive, string path, string content)
    {
        var entry = archive.CreateEntry(path);
        using var writer = new StreamWriter(entry.Open(), Encoding.UTF8);
        writer.Write(content.Trim());
    }
}

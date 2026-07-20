using System.Globalization;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Security;
using System.Text;
using FolioTrace.Aggregates;

namespace API.TradeFiles;

public sealed class TradeFileWorkbookGenerator
{
    public async Task<GeneratedTradeFile> GenerateAsync(
        TradeFileRequestedEvent request,
        DateTime generatedAtUtc,
        Stream output,
        CancellationToken cancellationToken = default)
    {
        if (!output.CanWrite || !output.CanSeek)
            throw new ArgumentException("TradeFile output must be writable and seekable.", nameof(output));

        var brokerName = new string(request.BrokerName.Where(character => char.IsLetterOrDigit(character) || character is '-' or '_').ToArray());
        var fileName = request.FileNameTemplate.Value
            .Replace("{brokername}", brokerName, StringComparison.OrdinalIgnoreCase)
            .Replace("{yyyymmddhhmmssnn}", generatedAtUtc.ToUniversalTime().ToString("yyyyMMddHHmmssff", CultureInfo.InvariantCulture), StringComparison.OrdinalIgnoreCase);
        using (var archive = new ZipArchive(output, ZipArchiveMode.Create, true))
        {
            Write(archive, "[Content_Types].xml", "<?xml version='1.0'?><Types xmlns='http://schemas.openxmlformats.org/package/2006/content-types'><Default Extension='rels' ContentType='application/vnd.openxmlformats-package.relationships+xml'/><Default Extension='xml' ContentType='application/xml'/><Override PartName='/xl/workbook.xml' ContentType='application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml'/><Override PartName='/xl/worksheets/sheet1.xml' ContentType='application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml'/></Types>");
            Write(archive, "_rels/.rels", "<?xml version='1.0'?><Relationships xmlns='http://schemas.openxmlformats.org/package/2006/relationships'><Relationship Id='rId1' Type='http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument' Target='xl/workbook.xml'/></Relationships>");
            Write(archive, "xl/workbook.xml", "<?xml version='1.0'?><workbook xmlns='http://schemas.openxmlformats.org/spreadsheetml/2006/main' xmlns:r='http://schemas.openxmlformats.org/officeDocument/2006/relationships'><sheets><sheet name='Trades' sheetId='1' r:id='rId1'/></sheets></workbook>");
            Write(archive, "xl/_rels/workbook.xml.rels", "<?xml version='1.0'?><Relationships xmlns='http://schemas.openxmlformats.org/package/2006/relationships'><Relationship Id='rId1' Type='http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet' Target='worksheets/sheet1.xml'/></Relationships>");
            WriteWorksheet(archive, request);
        }

        var contentLength = output.Length;
        output.Position = 0;
        var sha256 = Convert.ToHexString(await SHA256.HashDataAsync(output, cancellationToken));
        output.Position = 0;
        return new GeneratedTradeFile(fileName, contentLength, sha256);
    }

    private static void WriteWorksheet(ZipArchive archive, TradeFileRequestedEvent request)
    {
        using var writer = new StreamWriter(archive.CreateEntry("xl/worksheets/sheet1.xml", CompressionLevel.Fastest).Open(), new UTF8Encoding(false));
        writer.Write("<?xml version='1.0'?><worksheet xmlns='http://schemas.openxmlformats.org/spreadsheetml/2006/main'><sheetData>");
        writer.Write("<row r='1'>");
        for (var index = 0; index < request.Columns.Count; index++)
            writer.Write(TextCell(index, 1, request.Columns[index].ToString()));
        writer.Write("</row>");
        for (var rowIndex = 0; rowIndex < request.Tickets.Count; rowIndex++)
        {
            writer.Write("<row r='");
            writer.Write(rowIndex + 2);
            writer.Write("'>");
            for (var columnIndex = 0; columnIndex < request.Columns.Count; columnIndex++)
                writer.Write(Cell(columnIndex, rowIndex + 2, request.Columns[columnIndex], request.Tickets[rowIndex]));
            writer.Write("</row>");
        }
        writer.Write("</sheetData></worksheet>");
    }

    private static string Cell(int column, int row, TradeFileColumn type, TradeFileTicketSnapshot ticket) => type switch
    {
        TradeFileColumn.TicketID => NumberCell(column, row, ticket.TicketNumber.Value),
        TradeFileColumn.Quantity => NumberCell(column, row, ticket.Quantity),
        TradeFileColumn.Price => NumberCell(column, row, ticket.Price.Amount),
        TradeFileColumn.ISIN => TextCell(column, row, ticket.ISIN),
        TradeFileColumn.Sedol => TextCell(column, row, ticket.Sedol),
        TradeFileColumn.Currency => TextCell(column, row, ticket.Currency.Value),
        _ => TextCell(column, row, string.Empty)
    };

    private static string TextCell(int column, int row, string value) => "<c r='" + Column(column) + row + "' t='inlineStr'><is><t>" + SecurityElement.Escape(value) + "</t></is></c>";
    private static string NumberCell(int column, int row, decimal value) => "<c r='" + Column(column) + row + "'><v>" + value.ToString(CultureInfo.InvariantCulture) + "</v></c>";
    private static string Column(int index) { var result = string.Empty; for (index++; index > 0; index = (index - 1) / 26) result = (char)('A' + (index - 1) % 26) + result; return result; }
    private static void Write(ZipArchive archive, string path, string content) { using var writer = new StreamWriter(archive.CreateEntry(path, CompressionLevel.Fastest).Open(), new UTF8Encoding(false)); writer.Write(content); }
}

public sealed record GeneratedTradeFile(string FileName, long ContentLength, string SHA256);

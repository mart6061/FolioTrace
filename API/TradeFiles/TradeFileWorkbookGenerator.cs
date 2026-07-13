using System.Globalization;
using System.IO.Compression;
using System.Security;
using System.Text;
using FolioTrace.Aggregates;

namespace API.TradeFiles;

public sealed class TradeFileWorkbookGenerator
{
    public (string FileName, byte[] Content) Generate(TradeFileRequestedEvent request, DateTime generatedAtUtc)
    {
        var brokerName = new string(request.BrokerName.Where(character => char.IsLetterOrDigit(character) || character is '-' or '_').ToArray());
        var fileName = request.FileNameTemplate.Value
            .Replace("{brokername}", brokerName, StringComparison.OrdinalIgnoreCase)
            .Replace("{yyyymmddhhmmssnn}", generatedAtUtc.ToUniversalTime().ToString("yyyyMMddHHmmssff", CultureInfo.InvariantCulture), StringComparison.OrdinalIgnoreCase);
        using var output = new MemoryStream();
        using (var archive = new ZipArchive(output, ZipArchiveMode.Create, true))
        {
            Write(archive, "[Content_Types].xml", "<?xml version='1.0'?><Types xmlns='http://schemas.openxmlformats.org/package/2006/content-types'><Default Extension='rels' ContentType='application/vnd.openxmlformats-package.relationships+xml'/><Default Extension='xml' ContentType='application/xml'/><Override PartName='/xl/workbook.xml' ContentType='application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml'/><Override PartName='/xl/worksheets/sheet1.xml' ContentType='application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml'/></Types>");
            Write(archive, "_rels/.rels", "<?xml version='1.0'?><Relationships xmlns='http://schemas.openxmlformats.org/package/2006/relationships'><Relationship Id='rId1' Type='http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument' Target='xl/workbook.xml'/></Relationships>");
            Write(archive, "xl/workbook.xml", "<?xml version='1.0'?><workbook xmlns='http://schemas.openxmlformats.org/spreadsheetml/2006/main' xmlns:r='http://schemas.openxmlformats.org/officeDocument/2006/relationships'><sheets><sheet name='Trades' sheetId='1' r:id='rId1'/></sheets></workbook>");
            Write(archive, "xl/_rels/workbook.xml.rels", "<?xml version='1.0'?><Relationships xmlns='http://schemas.openxmlformats.org/package/2006/relationships'><Relationship Id='rId1' Type='http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet' Target='worksheets/sheet1.xml'/></Relationships>");
            Write(archive, "xl/worksheets/sheet1.xml", Worksheet(request));
        }
        return (fileName, output.ToArray());
    }

    private static string Worksheet(TradeFileRequestedEvent request)
    {
        var rows = new StringBuilder("<?xml version='1.0'?><worksheet xmlns='http://schemas.openxmlformats.org/spreadsheetml/2006/main'><sheetData>");
        rows.Append("<row r='1'>");
        for (var index = 0; index < request.Columns.Count; index++)
            rows.Append(TextCell(index, 1, request.Columns[index].ToString()));
        rows.Append("</row>");
        for (var rowIndex = 0; rowIndex < request.Tickets.Count; rowIndex++)
        {
            rows.Append("<row r='").Append(rowIndex + 2).Append("'>");
            for (var columnIndex = 0; columnIndex < request.Columns.Count; columnIndex++)
                rows.Append(Cell(columnIndex, rowIndex + 2, request.Columns[columnIndex], request.Tickets[rowIndex]));
            rows.Append("</row>");
        }
        return rows.Append("</sheetData></worksheet>").ToString();
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

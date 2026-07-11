using System.IO.Compression;
using API.TradeFiles;
using FolioTrace.Aggregates;
using FolioTrace.Types;

namespace Test;

public sealed class TradeFileWorkbookGeneratorTests
{
    [Fact]
    public void GeneratesDeterministicFilenameAndConfiguredColumns()
    {
        var generatedAt = new DateTime(2026, 7, 10, 12, 34, 56, 780, DateTimeKind.Utc);
        var request = new TradeFileRequestedEvent(
            new(Guid.NewGuid()), new(Guid.NewGuid()), new(generatedAt), new(generatedAt), "Test",
            new(Guid.NewGuid()), new("5493001KJTIIGC8Y1R12"), "North Bridge!",
            new("{brokername}-{yyyymmddhhmmssnn}.xlsx"),
            [TradeFileColumn.TicketID, TradeFileColumn.ISIN, TradeFileColumn.Quantity, TradeFileColumn.Price, TradeFileColumn.Currency],
            new FTPTradeMethodFileSendConfig("localhost", 21, "/incoming", "user", null),
            [new(new TicketNumber(42), "GB0002634946", "0263494", 100m, new Price(12.34m), new Alpha3("GBP"))]);

        var result = new TradeFileWorkbookGenerator().Generate(request, generatedAt);

        Assert.Equal("NorthBridge-2026071012345678.xlsx", result.FileName);
        using var archive = new ZipArchive(new MemoryStream(result.Content), ZipArchiveMode.Read);
        var sheet = archive.GetEntry("xl/worksheets/sheet1.xml");
        Assert.NotNull(sheet);
        using var reader = new StreamReader(sheet!.Open());
        var xml = reader.ReadToEnd();
        Assert.Contains("TicketID", xml);
        Assert.Contains("GB0002634946", xml);
        Assert.Contains(">100<", xml);
        Assert.Contains(">12.34<", xml);
        Assert.Contains("GBP", xml);
    }

    [Fact]
    public void GeneratesBlankCellsWhenISINAndSedolAreUnavailable()
    {
        var generatedAt = new DateTime(2026, 7, 10, 12, 34, 56, DateTimeKind.Utc);
        var request = new TradeFileRequestedEvent(
            new(Guid.NewGuid()), new(Guid.NewGuid()), new(generatedAt), new(generatedAt), "Test",
            new(Guid.NewGuid()), new("5493001KJTIIGC8Y1R12"), "North Bridge",
            new("{brokername}.xlsx"),
            [TradeFileColumn.ISIN, TradeFileColumn.Sedol],
            new FTPTradeMethodFileSendConfig("localhost", 21, "/incoming", "user", null),
            [new(new TicketNumber(42), string.Empty, string.Empty, 100m, new Price(12.34m), new Alpha3("GBP"))]);

        var result = new TradeFileWorkbookGenerator().Generate(request, generatedAt);

        using var archive = new ZipArchive(new MemoryStream(result.Content), ZipArchiveMode.Read);
        var sheet = archive.GetEntry("xl/worksheets/sheet1.xml");
        Assert.NotNull(sheet);
        using var reader = new StreamReader(sheet!.Open());
        var xml = reader.ReadToEnd();
        Assert.Contains("<c r='A2' t='inlineStr'><is><t></t></is></c>", xml);
        Assert.Contains("<c r='B2' t='inlineStr'><is><t></t></is></c>", xml);
    }
}

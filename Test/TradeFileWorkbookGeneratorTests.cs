using System.IO.Compression;
using API.TradeFiles;
using FolioTrace.Aggregates;
using FolioTrace.Types;

namespace Test;

public sealed class TradeFileWorkbookGeneratorTests
{
    [Fact]
    public async Task GeneratesDeterministicFilenameAndConfiguredColumns()
    {
        var generatedAt = new DateTime(2026, 7, 10, 12, 34, 56, 780, DateTimeKind.Utc);
        var request = new TradeFileRequestedEvent(
            new(Guid.NewGuid()), new(Guid.NewGuid()), new(generatedAt), new(generatedAt), "Test",
            new(Guid.NewGuid()), new("5493001KJTIIGC8Y1R12"), "North Bridge!",
            new("{brokername}-{yyyymmddhhmmssnn}.xlsx"),
            [TradeFileColumn.TicketID, TradeFileColumn.ISIN, TradeFileColumn.Quantity, TradeFileColumn.Price, TradeFileColumn.Currency],
            new FTPTradeMethodFileSendConfig("localhost", 21, "/incoming", "user", null),
            [new(new TicketNumber(42), "GB0002634946", "0263494", 100m, new Price(12.34m), new Alpha3("GBP"))]);

        await using var output = new MemoryStream();
        var result = await new TradeFileWorkbookGenerator().GenerateAsync(request, generatedAt, output);

        Assert.Equal("NorthBridge-2026071012345678.xlsx", result.FileName);
        Assert.Equal(output.Length, result.ContentLength);
        Assert.Equal(Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(output.ToArray())), result.SHA256);
        output.Position = 0;
        using var archive = new ZipArchive(output, ZipArchiveMode.Read, leaveOpen: true);
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
    public async Task GeneratesBlankCellsWhenISINAndSedolAreUnavailable()
    {
        var generatedAt = new DateTime(2026, 7, 10, 12, 34, 56, DateTimeKind.Utc);
        var request = new TradeFileRequestedEvent(
            new(Guid.NewGuid()), new(Guid.NewGuid()), new(generatedAt), new(generatedAt), "Test",
            new(Guid.NewGuid()), new("5493001KJTIIGC8Y1R12"), "North Bridge",
            new("{brokername}.xlsx"),
            [TradeFileColumn.ISIN, TradeFileColumn.Sedol],
            new FTPTradeMethodFileSendConfig("localhost", 21, "/incoming", "user", null),
            [new(new TicketNumber(42), string.Empty, string.Empty, 100m, new Price(12.34m), new Alpha3("GBP"))]);

        await using var output = new MemoryStream();
        await new TradeFileWorkbookGenerator().GenerateAsync(request, generatedAt, output);

        output.Position = 0;
        using var archive = new ZipArchive(output, ZipArchiveMode.Read, leaveOpen: true);
        var sheet = archive.GetEntry("xl/worksheets/sheet1.xml");
        Assert.NotNull(sheet);
        using var reader = new StreamReader(sheet!.Open());
        var xml = reader.ReadToEnd();
        Assert.Contains("<c r='A2' t='inlineStr'><is><t></t></is></c>", xml);
        Assert.Contains("<c r='B2' t='inlineStr'><is><t></t></is></c>", xml);
    }

    [Fact]
    public async Task GeneratesOptionTermsAndMultiplierAdjustedGrossPremium()
    {
        var generatedAt = new DateTime(2026, 7, 10, 12, 34, 56, DateTimeKind.Utc);
        var underlyingID = new InstrumentID(Guid.NewGuid());
        var option = new OptionExecutionDetails(
            OptionType.Put,
            underlyingID,
            "VOD",
            "GB00BH4HKS39",
            "4",
            new Money(70m, new Alpha3("GBP")),
            new InstrumentDate(new DateOnly(2026, 12, 18)),
            OptionExerciseStyle.European,
            OptionSettlementType.Cash,
            new ContractMultiplier(100m));
        var request = new TradeFileRequestedEvent(
            new(Guid.NewGuid()), new(Guid.NewGuid()), new(generatedAt), new(generatedAt), "Test",
            new(Guid.NewGuid()), new("5493001KJTIIGC8Y1R12"), "North Bridge",
            new("{brokername}.xlsx"),
            [TradeFileColumn.SecurityType, TradeFileColumn.OptionType, TradeFileColumn.UnderlyingISIN, TradeFileColumn.StrikePrice,
                TradeFileColumn.ExpirationDate, TradeFileColumn.ExerciseStyle, TradeFileColumn.SettlementType,
                TradeFileColumn.ContractMultiplier, TradeFileColumn.GrossPremium],
            new FTPTradeMethodFileSendConfig("localhost", 21, "/incoming", "user", null),
            [new(new TicketNumber(42), "", "", 3m, new Price(2.5m), new Alpha3("GBP"), "OPXXXX", option)]);

        await using var output = new MemoryStream();
        await new TradeFileWorkbookGenerator().GenerateAsync(request, generatedAt, output);

        output.Position = 0;
        using var archive = new ZipArchive(output, ZipArchiveMode.Read, leaveOpen: true);
        using var reader = new StreamReader(archive.GetEntry("xl/worksheets/sheet1.xml")!.Open());
        var xml = reader.ReadToEnd();
        Assert.Contains("OPT", xml);
        Assert.Contains("Put", xml);
        Assert.Contains("GB00BH4HKS39", xml);
        Assert.Contains("2026-12-18", xml);
        Assert.Contains("European", xml);
        Assert.Contains("Cash", xml);
        Assert.Contains(">100<", xml);
        Assert.Contains("<v>750", xml);
    }
}

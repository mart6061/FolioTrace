using FolioTrace.Aggregates;
using FolioTrace.Types;

namespace Repository.Seed;

internal static class SeedInstrumentData
{
    private const int SeedYears = 5;

    public static DateTime ValueStartDate => DateTime.UtcNow.Date.AddYears(-SeedYears);

    private static readonly (string Ticker, string Name, string Exchange, string Country, string Currency, decimal BasePrice)[] Seeds =
    [
        ("AZN", "AstraZeneca", "XLON", "GB", "GBP", 122.40m), ("SHEL", "Shell", "XLON", "GB", "GBP", 28.10m), ("HSBA", "HSBC Holdings", "XLON", "GB", "GBP", 7.15m), ("ULVR", "Unilever", "XLON", "GB", "GBP", 44.20m), ("BP", "BP", "XLON", "GB", "GBP", 4.80m),
        ("GSK", "GSK", "XLON", "GB", "GBP", 17.10m), ("RIO", "Rio Tinto", "XLON", "GB", "GBP", 54.00m), ("BATS", "British American Tobacco", "XLON", "GB", "GBP", 31.20m), ("DGE", "Diageo", "XLON", "GB", "GBP", 27.80m), ("LSEG", "London Stock Exchange Group", "XLON", "GB", "GBP", 92.00m),
        ("REL", "RELX", "XLON", "GB", "GBP", 34.60m), ("NG", "National Grid", "XLON", "GB", "GBP", 10.20m), ("BARC", "Barclays", "XLON", "GB", "GBP", 2.20m), ("LLOY", "Lloyds Banking Group", "XLON", "GB", "GBP", 0.58m), ("PRU", "Prudential", "XLON", "GB", "GBP", 7.80m),
        ("VOD", "Vodafone Group", "XLON", "GB", "GBP", 0.72m), ("TSCO", "Tesco", "XLON", "GB", "GBP", 3.05m), ("SSE", "SSE", "XLON", "GB", "GBP", 18.40m), ("AV", "Aviva", "XLON", "GB", "GBP", 4.80m), ("III", "3i Group", "XLON", "GB", "GBP", 29.50m),
        ("AAL", "Anglo American", "XLON", "GB", "GBP", 22.40m), ("ANTO", "Antofagasta", "XLON", "GB", "GBP", 21.30m), ("BA", "BAE Systems", "XLON", "GB", "GBP", 13.80m), ("BT-A", "BT Group", "XLON", "GB", "GBP", 1.35m), ("CNA", "Centrica", "XLON", "GB", "GBP", 1.40m),
        ("CPG", "Compass Group", "XLON", "GB", "GBP", 23.10m), ("CRDA", "Croda International", "XLON", "GB", "GBP", 48.30m), ("EXPN", "Experian", "XLON", "GB", "GBP", 35.60m), ("FLTR", "Flutter Entertainment", "XLON", "GB", "GBP", 160.00m), ("GLEN", "Glencore", "XLON", "GB", "GBP", 4.70m),
        ("HLMA", "Halma", "XLON", "GB", "GBP", 25.90m), ("IMB", "Imperial Brands", "XLON", "GB", "GBP", 21.70m), ("INF", "Informa", "XLON", "GB", "GBP", 8.20m), ("ITRK", "Intertek Group", "XLON", "GB", "GBP", 49.10m), ("JD", "JD Sports Fashion", "XLON", "GB", "GBP", 1.25m),
        ("KGF", "Kingfisher", "XLON", "GB", "GBP", 2.65m), ("LAND", "Land Securities", "XLON", "GB", "GBP", 6.40m), ("MKS", "Marks and Spencer", "XLON", "GB", "GBP", 2.90m), ("MNG", "M&G", "XLON", "GB", "GBP", 2.10m), ("NXT", "Next", "XLON", "GB", "GBP", 89.00m),
        ("PSN", "Persimmon", "XLON", "GB", "GBP", 13.40m), ("RKT", "Reckitt Benckiser", "XLON", "GB", "GBP", 46.80m), ("RR", "Rolls-Royce Holdings", "XLON", "GB", "GBP", 4.10m), ("SGE", "Sage Group", "XLON", "GB", "GBP", 10.90m), ("SMT", "Scottish Mortgage Investment Trust", "XLON", "GB", "GBP", 8.60m),
        ("SN", "Smith & Nephew", "XLON", "GB", "GBP", 10.30m), ("SPX", "Spirax Group", "XLON", "GB", "GBP", 92.50m), ("STAN", "Standard Chartered", "XLON", "GB", "GBP", 7.70m), ("TW", "Taylor Wimpey", "XLON", "GB", "GBP", 1.45m), ("WTB", "Whitbread", "XLON", "GB", "GBP", 31.50m),

        ("AAPL", "Apple", "XNAS", "US", "USD", 190.00m), ("MSFT", "Microsoft", "XNAS", "US", "USD", 420.00m), ("NVDA", "NVIDIA", "XNAS", "US", "USD", 930.00m), ("AMZN", "Amazon.com", "XNAS", "US", "USD", 180.00m), ("META", "Meta Platforms", "XNAS", "US", "USD", 485.00m),
        ("GOOGL", "Alphabet", "XNAS", "US", "USD", 165.00m), ("AVGO", "Broadcom", "XNAS", "US", "USD", 1320.00m), ("TSLA", "Tesla", "XNAS", "US", "USD", 175.00m), ("COST", "Costco Wholesale", "XNAS", "US", "USD", 720.00m), ("ADBE", "Adobe", "XNAS", "US", "USD", 485.00m),
        ("NFLX", "Netflix", "XNAS", "US", "USD", 610.00m), ("AMD", "Advanced Micro Devices", "XNAS", "US", "USD", 160.00m), ("PEP", "PepsiCo", "XNAS", "US", "USD", 175.00m), ("CSCO", "Cisco Systems", "XNAS", "US", "USD", 50.00m), ("INTC", "Intel", "XNAS", "US", "USD", 33.00m),
        ("QCOM", "Qualcomm", "XNAS", "US", "USD", 185.00m), ("TXN", "Texas Instruments", "XNAS", "US", "USD", 175.00m), ("AMAT", "Applied Materials", "XNAS", "US", "USD", 205.00m), ("HON", "Honeywell International", "XNAS", "US", "USD", 195.00m), ("INTU", "Intuit", "XNAS", "US", "USD", 625.00m),

        ("ASML", "ASML Holding", "XAMS", "NL", "EUR", 875.00m), ("SAP", "SAP", "XETR", "DE", "EUR", 175.00m), ("NESN", "Nestle", "XSWX", "CH", "CHF", 95.00m), ("NOVN", "Novartis", "XSWX", "CH", "CHF", 92.00m), ("ROG", "Roche Holding", "XSWX", "CH", "CHF", 255.00m),
        ("MC", "LVMH", "XPAR", "FR", "EUR", 760.00m), ("OR", "L'Oreal", "XPAR", "FR", "EUR", 430.00m), ("AIR", "Airbus", "XPAR", "FR", "EUR", 145.00m), ("SAN", "Sanofi", "XPAR", "FR", "EUR", 88.00m), ("TTE", "TotalEnergies", "XPAR", "FR", "EUR", 66.00m),
        ("SIE", "Siemens", "XETR", "DE", "EUR", 175.00m), ("ALV", "Allianz", "XETR", "DE", "EUR", 270.00m), ("DTE", "Deutsche Telekom", "XETR", "DE", "EUR", 23.00m), ("BNP", "BNP Paribas", "XPAR", "FR", "EUR", 63.00m), ("INGA", "ING Groep", "XAMS", "NL", "EUR", 14.00m),
        ("SANES", "Banco Santander", "XMAD", "ES", "EUR", 4.10m), ("IBE", "Iberdrola", "XMAD", "ES", "EUR", 12.00m), ("ENEL", "Enel", "XMIL", "IT", "EUR", 6.80m), ("ISP", "Intesa Sanpaolo", "XMIL", "IT", "EUR", 3.40m), ("NOKIA", "Nokia", "XHEL", "FI", "EUR", 3.60m),

        ("7203", "Toyota Motor", "XTKS", "JP", "JPY", 3600m), ("6758", "Sony Group", "XTKS", "JP", "JPY", 13000m), ("8306", "Mitsubishi UFJ Financial Group", "XTKS", "JP", "JPY", 1500m), ("6861", "Keyence", "XTKS", "JP", "JPY", 69000m), ("9984", "SoftBank Group", "XTKS", "JP", "JPY", 8200m)
    ];

    public static IReadOnlyList<InstrumentSeed> CreateInstrumentSeeds() =>
        Seeds.Select((seed, index) => new InstrumentSeed(
            InstrumentIDBuilder.Restore(CreateDeterministicGuid($"instrument-{seed.Ticker}-{index}")),
            seed.Ticker,
            seed.Name,
            $"{seed.Name} plc",
            seed.Exchange,
            seed.Country,
            seed.Currency,
            seed.BasePrice,
            CreateLogo(seed.Ticker, index),
            CreateSedol(index))).ToList();

    public static IEnumerable<InstrumentPriceSeed> CreatePriceSeeds(IReadOnlyList<InstrumentSeed> instruments)
    {
        var startDate = ValueStartDate.Date;
        var endDate = DateTime.UtcNow.Date;
        var latestAuditDateTime = DateTime.UtcNow.AddMinutes(-1);

        for (var date = startDate; date <= endDate; date = date.AddDays(1))
        {
            if (date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
                continue;

            var day = (date.Date - startDate).Days;

            foreach (var observationHour in new[] { 9, 12, 16 })
            {
                var timestamp = date.AddHours(observationHour);
                if (timestamp.AddMinutes(1) > latestAuditDateTime)
                    continue;

                for (var index = 0; index < instruments.Count; index++)
                {
                    var seed = instruments[index];
                    var wave = (decimal)Math.Sin((day + index) / 5.0) * 0.015m;
                    var intraday = (observationHour - 12) * 0.001m;
                    var mid = Round(seed.BasePrice * (1 + wave + intraday));
                    var spread = Math.Max(mid * 0.001m, 0.01m);
                    var currency = Alpha3Builder.Create(seed.Currency);
                    yield return new InstrumentPriceSeed(
                        seed.InstrumentID,
                        new InstrumentPriceEquity(
                            new Money(Round(mid - spread), currency),
                            new Money(mid, currency),
                            new Money(Round(mid + spread), currency),
                            new Money(mid, currency)),
                        timestamp);
                }
            }
        }
    }

    public static IEnumerable<InstrumentIncomeSeed> CreateIncomeSeeds(IReadOnlyList<InstrumentSeed> instruments) =>
        instruments.Select((seed, index) => new InstrumentIncomeSeed(
            seed.InstrumentID,
            new InstrumentIncomeEquity(new Money(Round(seed.BasePrice * (0.01m + (index % 5) * 0.002m)), Alpha3Builder.Create(seed.Currency))),
            ValueStartDate.Date.AddDays(14).AddHours(17)));

    private static decimal Round(decimal value) => decimal.Round(value, 4);

    private static string CreateSedol(int index) => $"B{index + 100000:000000}"[^7..];

    private static Guid CreateDeterministicGuid(string value)
    {
        var bytes = System.Security.Cryptography.MD5.HashData(System.Text.Encoding.UTF8.GetBytes(value));
        return new Guid(bytes);
    }

    private static InstrumentLogo CreateLogo(string ticker, int index)
    {
        var colours = new[] { "#0f766e", "#2563eb", "#7c3aed", "#be123c", "#b45309", "#047857", "#4338ca" };
        var colour = colours[index % colours.Length];
        var text = ticker.Length > 4 ? ticker[..4] : ticker;
        var svg = $"<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 640 480\"><rect width=\"640\" height=\"480\" rx=\"48\" fill=\"{colour}\"/><circle cx=\"512\" cy=\"112\" r=\"72\" fill=\"#ffffff\" opacity=\"0.18\"/><text x=\"320\" y=\"275\" text-anchor=\"middle\" font-family=\"Arial, Helvetica, sans-serif\" font-size=\"120\" font-weight=\"700\" fill=\"#ffffff\">{text}</text></svg>";
        return new InstrumentLogo(svg);
    }
}

internal sealed record InstrumentSeed(InstrumentID InstrumentID, string Ticker, string Name, string FormalName, string Exchange, string Country, string Currency, decimal BasePrice, InstrumentLogo Logo, string Sedol);

internal sealed record InstrumentPriceSeed(InstrumentID InstrumentID, InstrumentPriceEquity Price, DateTime Timestamp);

internal sealed record InstrumentIncomeSeed(InstrumentID InstrumentID, InstrumentIncomeEquity Income, DateTime Timestamp);

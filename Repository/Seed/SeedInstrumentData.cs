using FolioTrace.Aggregates;
using FolioTrace.Types;

namespace Repository.Seed;

internal static class SeedInstrumentData
{
    private const int SeedYears = 5;

    public static DateTime ValueStartDate => DateTime.UtcNow.Date.AddYears(-SeedYears);

    private static readonly (string Ticker, string Name, string Exchange, string Country, string Currency, decimal BasePrice)[] EquitySeeds =
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

    private static readonly (string Ticker, string Name, string Exchange, string Country, string Currency, decimal CleanPrice, decimal CouponRate, DateTime IssueDate, DateTime MaturityDate, string DayCount)[] BondSeeds =
    [
        ("UKT-2029", "UK Treasury 0.875% 2029", "XLON", "GB", "GBP", 96.42m, 0.00875m, new DateTime(2019, 1, 31), new DateTime(2029, 1, 31), "ACT/ACT"),
        ("UKT-2034", "UK Treasury 4.25% 2034", "XLON", "GB", "GBP", 101.18m, 0.0425m, new DateTime(2023, 9, 7), new DateTime(2034, 9, 7), "ACT/ACT"),
        ("UKT-2045", "UK Treasury 3.50% 2045", "XLON", "GB", "GBP", 94.76m, 0.035m, new DateTime(2014, 1, 22), new DateTime(2045, 1, 22), "ACT/ACT"),
        ("TSCO-2035", "Tesco 5.50% 2035", "XLON", "GB", "GBP", 103.64m, 0.055m, new DateTime(2024, 2, 27), new DateTime(2035, 2, 27), "30/360"),
        ("VOD-2056", "Vodafone 3.00% 2056", "XLON", "GB", "GBP", 82.35m, 0.03m, new DateTime(2016, 8, 12), new DateTime(2056, 8, 12), "30/360"),
        ("NG-2030", "National Grid 4.25% 2030", "XLON", "GB", "GBP", 99.88m, 0.0425m, new DateTime(2022, 4, 5), new DateTime(2030, 4, 5), "30/360")
    ];

    private static readonly (string Ticker, string Name, string Exchange, string Country, string Currency)[] CashSeeds =
    [
        ("GBP-CASH", "British Pound Cash", "CASH", "GB", "GBP")
    ];

    public static IReadOnlyList<InstrumentSeed> CreateInstrumentSeeds() =>
        EquitySeeds
            .Select((seed, index) => new InstrumentSeed(
                InstrumentIDBuilder.Restore(CreateDeterministicGuid($"instrument-equity-{seed.Ticker}-{index}")),
                InstrumentSeedKind.Equity,
                seed.Ticker,
                seed.Name,
                $"{seed.Name} plc",
                seed.Exchange,
                seed.Country,
                seed.Currency,
                "ESVUFR",
                seed.BasePrice,
                new InstrumentTermsEquity(),
                CreateLogo(seed.Ticker, index),
                CreateSedol(index)))
            .Concat(BondSeeds.Select((seed, index) => new InstrumentSeed(
                InstrumentIDBuilder.Restore(CreateDeterministicGuid($"instrument-bond-{seed.Ticker}-{index}")),
                InstrumentSeedKind.FixedIncome,
                seed.Ticker,
                seed.Name,
                seed.Name,
                seed.Exchange,
                seed.Country,
                seed.Currency,
                "DBFUFR",
                seed.CleanPrice,
                CreateBondTerms(seed.Currency, seed.CouponRate, seed.IssueDate, seed.MaturityDate, seed.DayCount),
                CreateLogo(seed.Ticker, EquitySeeds.Length + index),
                CreateSedol(EquitySeeds.Length + index))))
            .Concat(CashSeeds.Select((seed, index) => new InstrumentSeed(
                InstrumentIDBuilder.Restore(CreateDeterministicGuid($"instrument-cash-{seed.Ticker}-{index}")),
                InstrumentSeedKind.Cash,
                seed.Ticker,
                seed.Name,
                seed.Name,
                seed.Exchange,
                seed.Country,
                seed.Currency,
                "MRCXXX",
                1m,
                null,
                CreateLogo(seed.Ticker, EquitySeeds.Length + BondSeeds.Length + index),
                CreateSedol(EquitySeeds.Length + BondSeeds.Length + index))))
            .ToList();

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
                    var wave = seed.Kind == InstrumentSeedKind.FixedIncome
                        ? (decimal)Math.Sin((day + index) / 13.0) * 0.004m
                        : (decimal)Math.Sin((day + index) / 5.0) * 0.015m;
                    var intraday = seed.Kind == InstrumentSeedKind.FixedIncome
                        ? (observationHour - 12) * 0.0002m
                        : (observationHour - 12) * 0.001m;
                    var mid = Round(seed.BasePrice * (1 + wave + intraday));
                    var spread = Math.Max(mid * 0.001m, 0.01m);
                    yield return new InstrumentPriceSeed(
                        seed.InstrumentID,
                        CreatePrice(seed, mid, spread),
                        timestamp);
                }
            }
        }
    }

    public static IEnumerable<InstrumentIncomeSeed> CreateIncomeSeeds(IReadOnlyList<InstrumentSeed> instruments) =>
        instruments.Select((seed, index) => new InstrumentIncomeSeed(
            seed.InstrumentID,
            CreateIncome(seed, index),
            ValueStartDate.Date.AddDays(14).AddHours(17)));

    private static IInstrumentPrice CreatePrice(InstrumentSeed seed, decimal mid, decimal spread) =>
        seed.Kind switch
        {
            InstrumentSeedKind.Equity => new InstrumentPriceEquity(
                new InstrumentPrice(Round(mid - spread)),
                new InstrumentPrice(mid),
                new InstrumentPrice(Round(mid + spread)),
                new InstrumentPrice(mid)),
            InstrumentSeedKind.FixedIncome => new InstrumentPriceFixedIncome(new ValuationPrice(mid)),
            InstrumentSeedKind.Cash => new InstrumentPriceCash(),
            _ => throw new InvalidOperationException($"Unsupported instrument seed kind '{seed.Kind}'.")
        };

    private static IInstrumentIncome CreateIncome(InstrumentSeed seed, int index) =>
        seed.Kind switch
        {
            InstrumentSeedKind.Equity => CreateEquityIncome(seed, index),
            InstrumentSeedKind.FixedIncome => CreateFixedIncomeIncome(seed, index),
            InstrumentSeedKind.Cash => new InstrumentIncomeCash(),
            _ => throw new InvalidOperationException($"Unsupported instrument seed kind '{seed.Kind}'.")
        };

    private static InstrumentIncomeEquity CreateEquityIncome(InstrumentSeed seed, int index)
    {
        var declaration = DateOnly.FromDateTime(ValueStartDate.Date.AddDays(1 + index % 5));
        var exDividend = declaration.AddDays(14);
        var record = exDividend.AddDays(1);
        var payable = record.AddDays(21);

        return new InstrumentIncomeEquity(
            new InstrumentPrice(Round(seed.BasePrice * (0.01m + (index % 5) * 0.002m))),
            index % 4 == 0 ? "Special" : "Regular",
            InstrumentDateBuilder.Create(exDividend),
            InstrumentDateBuilder.Create(declaration),
            InstrumentDateBuilder.Create(record),
            InstrumentDateBuilder.Create(payable));
    }

    private static InstrumentIncomeFixedIncome CreateFixedIncomeIncome(InstrumentSeed seed, int index)
    {
        var couponRate = seed.Terms is InstrumentTermsBond bondTerms ? bondTerms.CouponRate.Value : 0m;
        var accruedDays = 15 + index % 45;
        var accruedInterest = Round(100m * couponRate * accruedDays / 365m);

        return new InstrumentIncomeFixedIncome(new ValuationPrice(accruedInterest));
    }

    private static InstrumentTermsBond CreateBondTerms(string currency, decimal couponRate, DateTime issueDate, DateTime maturityDate, string dayCount) =>
        new(
            new Money(100m, Alpha3Builder.Create(currency)),
            new CouponRate(couponRate),
            CouponFrequency.SemiAnnual,
            maturityDate,
            issueDate,
            dayCount);

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

internal enum InstrumentSeedKind
{
    Equity,
    FixedIncome,
    Cash
}

internal sealed record InstrumentSeed(InstrumentID InstrumentID, InstrumentSeedKind Kind, string Ticker, string Name, string FormalName, string Exchange, string Country, string Currency, string Cfi, decimal BasePrice, IInstrumentTerms? Terms, InstrumentLogo Logo, string Sedol);

internal sealed record InstrumentPriceSeed(InstrumentID InstrumentID, IInstrumentPrice Price, DateTime Timestamp);

internal sealed record InstrumentIncomeSeed(InstrumentID InstrumentID, IInstrumentIncome Income, DateTime Timestamp);

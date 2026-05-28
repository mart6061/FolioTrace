using FolioTrace.Aggregates;
using FolioTrace.Types;

namespace Repository.Seed;

internal static class SeedFXData
{
    private const int SeedMonths = 12;

    public static DateTime RateStartDate => DateTime.UtcNow.Date.AddMonths(-SeedMonths);

    private static readonly string[] SeedCurrencies =
    [
        "EUR",
        "GBP",
        "USD",
        "CAD",
        "MXN",
        "CHF",
        "JPY",
        "CNY",
        "HKD",
        "SGD",
        "INR",
        "KRW",
        "THB",
        "AED",
        "SAR",
        "QAR",
        "KWD",
        "ILS",
        "TRY",
        "AUD",
        "NZD"
    ];

    private static readonly Dictionary<string, decimal> UsdPerUnit = new(StringComparer.Ordinal)
    {
        ["AED"] = 0.2723m,
        ["AUD"] = 0.6600m,
        ["CAD"] = 0.7350m,
        ["CHF"] = 1.1200m,
        ["CNY"] = 0.1380m,
        ["EUR"] = 1.0800m,
        ["GBP"] = 1.2700m,
        ["HKD"] = 0.1280m,
        ["ILS"] = 0.2700m,
        ["INR"] = 0.0120m,
        ["JPY"] = 0.0067m,
        ["KRW"] = 0.00074m,
        ["KWD"] = 3.2500m,
        ["MXN"] = 0.0590m,
        ["NZD"] = 0.6100m,
        ["QAR"] = 0.2747m,
        ["SAR"] = 0.2666m,
        ["SGD"] = 0.7400m,
        ["THB"] = 0.0275m,
        ["TRY"] = 0.0310m,
        ["USD"] = 1.0000m
    };

    public static IReadOnlyList<FXPairSeed> CreatePairSeeds()
    {
        var pairs = new List<FXPairSeed>();
        var included = new HashSet<string>(StringComparer.Ordinal);

        AddHub("GBP");
        AddHub("EUR");
        AddHub("USD");

        AddBothWays("AUD", "NZD");
        AddBothWays("CAD", "MXN");

        foreach (var code in new[] { "CNY", "HKD", "INR", "JPY", "KRW", "THB" })
            AddBothWays("SGD", code);

        foreach (var code in new[] { "ILS", "KWD", "QAR", "SAR", "TRY" })
            AddBothWays("AED", code);

        return pairs;

        void AddHub(string hub)
        {
            foreach (var code in SeedCurrencies.Where(code => code != hub))
                AddBothWays(hub, code);
        }

        void AddBothWays(string left, string right)
        {
            Add(left, right);
            Add(right, left);
        }

        void Add(string baseCurrency, string quoteCurrency)
        {
            var key = $"{baseCurrency}{quoteCurrency}";
            if (baseCurrency == quoteCurrency || !included.Add(key))
                return;

            pairs.Add(new FXPairSeed(baseCurrency, quoteCurrency, CalculateBaselineMid(baseCurrency, quoteCurrency)));
        }
    }

    public static IEnumerable<FXRateSeed> CreateRateSeeds(IReadOnlyList<FXPairSeed> pairSeeds)
    {
        if (pairSeeds is null)
            throw new ArgumentNullException(nameof(pairSeeds));

        var startDate = RateStartDate;
        var endDate = DateTime.UtcNow.Date;
        var latestAuditDateTime = DateTime.UtcNow.AddMinutes(-1);
        var observationTimes = new[]
        {
            new TimeSpan(9, 0, 0),
            new TimeSpan(12, 0, 0),
            new TimeSpan(16, 0, 0)
        };

        for (var date = startDate; date <= endDate; date = date.AddDays(1))
        {
            if (date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
                continue;

            var dayIndex = (date.Date - startDate.Date).Days;

            foreach (var observationTime in observationTimes)
            {
                var observationIndex = Array.IndexOf(observationTimes, observationTime);
                var timestamp = date.Date.Add(observationTime);
                if (timestamp.AddMinutes(1) > latestAuditDateTime)
                    continue;

                foreach (var pairSeed in pairSeeds)
                    yield return new FXRateSeed(
                        pairSeed.Pair,
                        CreatePrice(pairSeed, dayIndex, observationIndex),
                        timestamp);
            }
        }
    }

    private static decimal CalculateBaselineMid(string baseCurrency, string quoteCurrency) =>
        RoundPrice(UsdPerUnit[baseCurrency] / UsdPerUnit[quoteCurrency]);

    private static FXPrice CreatePrice(FXPairSeed pairSeed, int dayIndex, int observationIndex)
    {
        var pairOffset = GetPairOffset(pairSeed.Pair);
        var wave = (decimal)Math.Sin((dayIndex + pairOffset) / 19.0) * 0.018m;
        var slowWave = (decimal)Math.Cos((dayIndex + pairOffset) / 71.0) * 0.011m;
        var intradayMove = (observationIndex - 1) * 0.00035m;
        var mid = RoundPrice(pairSeed.BaselineMid * (1 + wave + slowWave + intradayMove));
        var spread = RoundPrice(Math.Max(mid * 0.00035m, 0.00000001m));
        var bid = RoundPrice(mid - spread);
        var ask = RoundPrice(mid + spread);

        return new FXPrice(new Bid(bid), new Mid(mid), new Ask(ask));
    }

    private static int GetPairOffset(CurrencyPair pair) =>
        pair.Value.Aggregate(0, (total, character) => total + character);

    private static decimal RoundPrice(decimal value) =>
        decimal.Round(value, 8, MidpointRounding.AwayFromZero);
}

internal sealed record FXPairSeed(string BaseCurrency, string QuoteCurrency, decimal BaselineMid)
{
    public CurrencyPair Pair { get; } = new(BaseCurrency, QuoteCurrency);
}

internal sealed record FXRateSeed(CurrencyPair Pair, FXPrice Price, DateTime Timestamp);

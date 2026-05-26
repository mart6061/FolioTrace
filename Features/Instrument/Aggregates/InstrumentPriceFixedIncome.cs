using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record InstrumentPriceFixedIncome : IInstrumentPrice
{
    public required ValuationPrice CleanPrice { get; init; }

    public string PriceType => nameof(InstrumentPriceFixedIncome);

    [JsonConstructor]
    [SetsRequiredMembers]
    public InstrumentPriceFixedIncome(ValuationPrice cleanPrice)
    {
        CleanPrice = cleanPrice ?? throw new ArgumentNullException(nameof(cleanPrice));
    }

    public string ToData() => CleanPrice.ToData();

    public string ToDetail() => $"{nameof(InstrumentPriceFixedIncome)}: (CleanPrice: {CleanPrice.ToDetail()})";
}

using System.ComponentModel;
using System.Text.Json.Serialization;

namespace FolioTrace.Aggregates;

/// <summary>
/// Chooses whether a price is shown clean or dirty. Orthogonal to <see cref="InstrumentPriceBasis"/>, which
/// chooses which quote; this chooses whether accrued interest is in it. Only fixed income accrues, so the
/// convention makes no difference anywhere else.
/// </summary>
/// <remarks>
/// This is a presentation choice, not a different valuation: the clean subtotal plus total accrued interest
/// equals the dirty total, and a test asserts it.
/// </remarks>
[JsonConverter(typeof(JsonStringEnumConverter<ValuationPriceConvention>))]
public enum ValuationPriceConvention
{
    [Description("Clean price, with accrued interest shown separately")]
    Clean,

    [Description("Dirty price, including accrued interest")]
    Dirty
}

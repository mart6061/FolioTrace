using System.Text.Json.Serialization;

namespace FolioTrace.Aggregates;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(FIXTradeMethod), nameof(FIXTradeMethod))]
[JsonDerivedType(typeof(PhoneTradeMethod), nameof(PhoneTradeMethod))]
[JsonDerivedType(typeof(FaxTradeMethod), nameof(FaxTradeMethod))]
[JsonDerivedType(typeof(TradeFileTradeMethod), nameof(TradeFileTradeMethod))]
[JsonDerivedType(typeof(ManualTradeMethod), nameof(ManualTradeMethod))]
public interface ITradeMethod
{
    TradeMethodType Type { get; }
}

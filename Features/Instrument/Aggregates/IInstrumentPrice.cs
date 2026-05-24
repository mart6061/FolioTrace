using System.Text.Json.Serialization;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

[JsonConverter(typeof(InstrumentPriceJsonConverter))]
public interface IInstrumentPrice : IType
{
    string PriceType { get; }
}

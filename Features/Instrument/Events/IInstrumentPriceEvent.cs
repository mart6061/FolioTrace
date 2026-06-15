using System.Text.Json.Serialization;
using FolioTrace.Common;

namespace FolioTrace.Aggregates;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(InstrumentPriceSetEvent), nameof(InstrumentPriceSetEvent))]
public interface IInstrumentPriceEvent : IEventBase;

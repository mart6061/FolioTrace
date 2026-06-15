using System.Text.Json.Serialization;
using FolioTrace.Common;

namespace FolioTrace.Aggregates;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(InstrumentIncomeSetEvent), nameof(InstrumentIncomeSetEvent))]
public interface IInstrumentIncomeEvent : IEventBase;

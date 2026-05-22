using System.Text.Json.Serialization;
using FolioTrace.Common;

namespace FolioTrace.Aggregates;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(InstrumentCreatedEvent), nameof(InstrumentCreatedEvent))]
[JsonDerivedType(typeof(InstrumentModifiedEvent), nameof(InstrumentModifiedEvent))]
[JsonDerivedType(typeof(InstrumentActiveModifiedEvent), nameof(InstrumentActiveModifiedEvent))]
[JsonDerivedType(typeof(InstrumentIdentifierSetEvent), nameof(InstrumentIdentifierSetEvent))]
[JsonDerivedType(typeof(InstrumentIdentifierUnsetEvent), nameof(InstrumentIdentifierUnsetEvent))]
[JsonDerivedType(typeof(InstrumentTermsSetEvent), nameof(InstrumentTermsSetEvent))]
public interface IInstrumentEvent : IEventBase;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(InstrumentPriceSetEvent), nameof(InstrumentPriceSetEvent))]
public interface IInstrumentPriceEvent : IEventBase;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(InstrumentIncomeSetEvent), nameof(InstrumentIncomeSetEvent))]
public interface IInstrumentIncomeEvent : IEventBase;

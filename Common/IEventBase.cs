using System.Text.Json.Serialization;
using FolioTrace.Aggregates;
using FolioTrace.Types;

namespace FolioTrace.Common;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(CountryCreatedEvent), nameof(CountryCreatedEvent))]
[JsonDerivedType(typeof(CountryModifiedEvent), nameof(CountryModifiedEvent))]
[JsonDerivedType(typeof(CountryFlagModifiedEvent), nameof(CountryFlagModifiedEvent))]
[JsonDerivedType(typeof(CurrencyCreatedEvent), nameof(CurrencyCreatedEvent))]
[JsonDerivedType(typeof(CurrencyModifiedEvent), nameof(CurrencyModifiedEvent))]
[JsonDerivedType(typeof(FXCreatedEvent), nameof(FXCreatedEvent))]
[JsonDerivedType(typeof(FXActiveModifiedEvent), nameof(FXActiveModifiedEvent))]
[JsonDerivedType(typeof(FXRateSetEvent), nameof(FXRateSetEvent))]
[JsonDerivedType(typeof(InstrumentCreatedEvent), nameof(InstrumentCreatedEvent))]
[JsonDerivedType(typeof(InstrumentModifiedEvent), nameof(InstrumentModifiedEvent))]
[JsonDerivedType(typeof(InstrumentActiveModifiedEvent), nameof(InstrumentActiveModifiedEvent))]
[JsonDerivedType(typeof(InstrumentIdentifierSetEvent), nameof(InstrumentIdentifierSetEvent))]
[JsonDerivedType(typeof(InstrumentIdentifierUnsetEvent), nameof(InstrumentIdentifierUnsetEvent))]
[JsonDerivedType(typeof(InstrumentTermsSetEvent), nameof(InstrumentTermsSetEvent))]
[JsonDerivedType(typeof(InstrumentPriceSetEvent), nameof(InstrumentPriceSetEvent))]
[JsonDerivedType(typeof(InstrumentIncomeSetEvent), nameof(InstrumentIncomeSetEvent))]
[JsonDerivedType(typeof(UserCreatedEvent), nameof(UserCreatedEvent))]
[JsonDerivedType(typeof(UserModifiedEvent), nameof(UserModifiedEvent))]
public interface IEventBase : IType
{
    string Type { get; }

    EventID EventID { get; }

    UserID UserID { get; }

    EventDateTime EventDateTime { get; }

    AuditDateTime AuditDateTime { get; }

    string Reason { get; }
}

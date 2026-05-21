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
[JsonDerivedType(typeof(UserCreatedEvent), nameof(UserCreatedEvent))]
[JsonDerivedType(typeof(UserModifiedEvent), nameof(UserModifiedEvent))]
public abstract record EventBase(EventID EventID, UserID UserID, EventDateTime EventDateTime, AuditDateTime AuditDateTime, string Reason) : IEventBase
{
    [JsonIgnore]
    public Guid Id => EventID.Value;

    [JsonPropertyName("$type")]
    public abstract string Type { get; }

    public virtual string ToData() => $"{EventID.ToData()}|{UserID.ToData()}|{EventDateTime.ToData()}|{AuditDateTime.ToData()}|{Reason}";

    public  virtual string ToDetail() => $"{nameof(EventBase)}: ({EventID.ToDetail()}, {UserID.ToDetail()}, {EventDateTime.ToDetail()}, {AuditDateTime.ToDetail()}, Reason: {Reason})";
}

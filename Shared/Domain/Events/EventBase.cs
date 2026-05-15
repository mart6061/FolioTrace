using System.Text.Json.Serialization;
using AILibrary.Types;

namespace AILibrary.Domain;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(CountryCreatedEvent), nameof(CountryCreatedEvent))]
[JsonDerivedType(typeof(CountryModifiedEvent), nameof(CountryModifiedEvent))]
[JsonDerivedType(typeof(CurrencyCreatedEvent), nameof(CurrencyCreatedEvent))]
[JsonDerivedType(typeof(CurrencyModifiedEvent), nameof(CurrencyModifiedEvent))]
public abstract record EventBase(EventID EventID, EventDateTime EventDateTime, AuditDateTime AuditDateTime, string Reason) : IEventBase
{
    [JsonPropertyName("$type")]
    public abstract string Type { get; }

    public virtual string ToData() => $"{EventID.ToData()}|{EventDateTime.ToData()}|{AuditDateTime.ToData()}|{Reason}";

    public  virtual string ToDetail() => $"{nameof(EventBase)}: ({EventID.ToDetail()}, {EventDateTime.ToDetail()}, {AuditDateTime.ToDetail()}, Reason: {Reason})";
}

using AILibrary.Common;
using AILibrary.Types;

namespace AILibrary.Aggregates;

public sealed record CurrencyModifiedEvent(
    EventID EventID,
    EventDateTime EventDateTime,
    AuditDateTime AuditDateTime,
    string Reason,
    ISO3 AlphabeticCode,
    int NumericCode,
    short DecimalPlace,
    string Name
) : EventBase(EventID, EventDateTime, AuditDateTime, Reason)
{
    public override string Type => nameof(CurrencyModifiedEvent); // TODO: Remind me to create a universal constant for this event type.

    public override string ToData() =>
        $"{base.ToData()}|{AlphabeticCode.ToData()}|{NumericCode}|{DecimalPlace}|{Name}";

    public override string ToDetail() =>
        $"{nameof(CurrencyModifiedEvent)}: ({base.ToDetail()}, AlphabeticCode: {AlphabeticCode.ToDetail()}, NumericCode: {NumericCode}, DecimalPlace: {DecimalPlace}, Name: {Name})";
}

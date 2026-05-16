using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record CurrencyCreatedEvent(
    EventID EventID,
    EventDateTime EventDateTime,
    AuditDateTime AuditDateTime,
    string Reason,
    Alpha3 AlphabeticCode,
    int NumericCode,
    short DecimalPlace,
    string Name
) : EventBase(EventID, EventDateTime, AuditDateTime, Reason)
{
    public override string Type => nameof(CurrencyCreatedEvent); // TODO: Remind me to create a universal constant for this event type.

    public override string ToData() =>
        $"{base.ToData()}|{AlphabeticCode.ToData()}|{NumericCode}|{DecimalPlace}|{Name}";

    public override string ToDetail() =>
        $"{nameof(CurrencyCreatedEvent)}: ({base.ToDetail()}, AlphabeticCode: {AlphabeticCode.ToDetail()}, NumericCode: {NumericCode}, DecimalPlace: {DecimalPlace}, Name: {Name})";
}

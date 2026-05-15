using AILibrary.Types;

namespace AILibrary.Domain;

public sealed record CountryModifiedEvent(
    EventID EventID,
    EventDateTime EventDateTime,
    AuditDateTime AuditDateTime,
    string Reason,
    ISO2 ISO2,
    ISO3 ISO3,
    short Numeric
) : EventBase(EventID, EventDateTime, AuditDateTime, Reason)
{
    public override string Type => nameof(CountryModifiedEvent); // TODO: Remind me to create a universal constant for this event type.

    public override string ToData() =>
        $"{base.ToData()}|{ISO2.ToData()}|{ISO3.ToData()}|{Numeric}";

    public override string ToDetail() =>
        $"{nameof(CountryModifiedEvent)}: ({base.ToDetail()}, ISO2: {ISO2.ToDetail()}, ISO3: {ISO3.ToDetail()}, Numeric: {Numeric})";
}

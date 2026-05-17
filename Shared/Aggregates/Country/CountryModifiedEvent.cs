using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record CountryModifiedEvent(
    EventID EventID,
    EventDateTime EventDateTime,
    AuditDateTime AuditDateTime,
    string Reason,
    Alpha2 Alpha2,
    Alpha3 Alpha3,
    short Numeric,
    string Name
) : EventBase(EventID, EventDateTime, AuditDateTime, Reason), ICountryEvent
{
    public override string Type => nameof(CountryModifiedEvent); // TODO: Remind me to create a universal constant for this event type.

    public override string ToData() =>
        $"{base.ToData()}|{Alpha2.ToData()}|{Alpha3.ToData()}|{Numeric}|{Name}";

    public override string ToDetail() =>
        $"{nameof(CountryModifiedEvent)}: ({base.ToDetail()}, Alpha2: {Alpha2.ToDetail()}, Alpha3: {Alpha3.ToDetail()}, Numeric: {Numeric}, Name: {Name})";
}

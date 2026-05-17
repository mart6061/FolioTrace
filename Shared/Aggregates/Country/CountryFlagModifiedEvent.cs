using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record CountryFlagModifiedEvent(
    EventID EventID,
    EventDateTime EventDateTime,
    AuditDateTime AuditDateTime,
    string Reason,
    Alpha2 Alpha2,
    CountryFlag Flag
) : EventBase(EventID, EventDateTime, AuditDateTime, Reason), ICountryEvent
{
    public override string Type => nameof(CountryFlagModifiedEvent); // TODO: Remind me to create a universal constant for this event type.

    public override string ToData() =>
        $"{base.ToData()}|{Alpha2.ToData()}|{Flag.ToData()}";

    public override string ToDetail() =>
        $"{nameof(CountryFlagModifiedEvent)}: ({base.ToDetail()}, Alpha2: {Alpha2.ToDetail()}, Flag: {Flag.ToDetail()})";
}

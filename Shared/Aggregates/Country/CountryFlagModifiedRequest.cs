using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record CountryFlagModifiedRequest(
    UserID UserID,
    EventDateTime EventDateTime,
    string Reason,
    Alpha2 Alpha2,
    CountryFlag Flag) : IEventRequest;

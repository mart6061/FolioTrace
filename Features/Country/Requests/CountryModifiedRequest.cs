using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record CountryModifiedRequest(
    UserID UserID,
    EventDateTime EventDateTime,
    string Reason,
    Alpha2 Alpha2,
    Alpha3 Alpha3,
    short Numeric,
    string Name) : IEventRequest;

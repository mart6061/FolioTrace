using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record BrokerModifiedRequest(
    UserID UserID,
    EventDateTime EventDateTime,
    string Reason,
    LegalEntityIdentifier LEI,
    string Name,
    FeeRate Commission) : IEventRequest;

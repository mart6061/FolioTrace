using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record BrokerActiveSetRequest(
    UserID UserID,
    EventDateTime EventDateTime,
    string Reason,
    LegalEntityIdentifier LEI,
    bool Active) : IEventRequest;

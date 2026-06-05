using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record BrokerApprovedDateTimeSetRequest(
    UserID UserID,
    EventDateTime EventDateTime,
    string Reason,
    LegalEntityIdentifier LEI,
    EventDateTime ApprovedDateTime) : IEventRequest;

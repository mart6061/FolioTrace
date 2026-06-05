using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record BrokerCreatedRequest(
    UserID UserID,
    EventDateTime EventDateTime,
    string Reason,
    string Name,
    LegalEntityIdentifier LEI,
    FeeRate Commission,
    bool Active,
    EventDateTime ApprovedDateTime,
    EventDateTime NextReview,
    string Notes) : IEventRequest;

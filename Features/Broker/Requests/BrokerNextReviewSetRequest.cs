using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record BrokerNextReviewSetRequest(
    UserID UserID,
    EventDateTime EventDateTime,
    string Reason,
    LegalEntityIdentifier LEI,
    EventDateTime NextReview) : IEventRequest;

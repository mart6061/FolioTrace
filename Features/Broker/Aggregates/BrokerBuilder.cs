using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public static class BrokerBuilder
{
    public static Broker Create(UserID userId, string name, LegalEntityIdentifier lei, FeeRate commission, Active active, EventDateTime approvedDateTime, EventDateTime nextReview, string notes)
    {
        var createdEvent = BrokerCreatedEventBuilder.Create(
            userId,
            EventDateTimeBuilder.Create(),
            $"Create {nameof(Broker)}",
            name,
            lei,
            commission,
            active,
            approvedDateTime,
            nextReview,
            notes);

        return Create(createdEvent.Value!);
    }

    public static Broker Create(BrokerCreatedEvent createdEvent)
    {
        if (createdEvent is null)
            throw new ArgumentNullException(nameof(createdEvent));

        return new Broker(createdEvent.Name, createdEvent.LEI, createdEvent.Commission, createdEvent.Active, createdEvent.ApprovedDateTime, createdEvent.NextReview, createdEvent.Notes, createdEvent.EventDateTime, createdEvent.AuditDateTime, createdEvent.EventID);
    }

    extension(Broker broker)
    {
        public Broker Apply(BrokerModifiedEvent modifiedEvent)
        {
            if (broker is null)
                throw new ArgumentNullException(nameof(broker));

            if (modifiedEvent is null)
                throw new ArgumentNullException(nameof(modifiedEvent));

            return broker with
            {
                Name = modifiedEvent.Name,
                Commission = modifiedEvent.Commission,
                ValuationDateTime = modifiedEvent.EventDateTime,
                AsOfDateTime = modifiedEvent.AuditDateTime,
                LastEventID = modifiedEvent.EventID,
                LastAuditDateTime = modifiedEvent.AuditDateTime
            };
        }

        public Broker Apply(BrokerActiveSetEvent activeSetEvent)
        {
            if (broker is null)
                throw new ArgumentNullException(nameof(broker));

            if (activeSetEvent is null)
                throw new ArgumentNullException(nameof(activeSetEvent));

            return broker with
            {
                Active = activeSetEvent.Active,
                ValuationDateTime = activeSetEvent.EventDateTime,
                AsOfDateTime = activeSetEvent.AuditDateTime,
                LastEventID = activeSetEvent.EventID,
                LastAuditDateTime = activeSetEvent.AuditDateTime
            };
        }

        public Broker Apply(BrokerApprovedDateTimeSetEvent approvedDateTimeSetEvent)
        {
            if (broker is null)
                throw new ArgumentNullException(nameof(broker));

            if (approvedDateTimeSetEvent is null)
                throw new ArgumentNullException(nameof(approvedDateTimeSetEvent));

            return broker with
            {
                ApprovedDateTime = approvedDateTimeSetEvent.ApprovedDateTime,
                ValuationDateTime = approvedDateTimeSetEvent.EventDateTime,
                AsOfDateTime = approvedDateTimeSetEvent.AuditDateTime,
                LastEventID = approvedDateTimeSetEvent.EventID,
                LastAuditDateTime = approvedDateTimeSetEvent.AuditDateTime
            };
        }

        public Broker Apply(BrokerNextReviewSetEvent nextReviewSetEvent)
        {
            if (broker is null)
                throw new ArgumentNullException(nameof(broker));

            if (nextReviewSetEvent is null)
                throw new ArgumentNullException(nameof(nextReviewSetEvent));

            return broker with
            {
                NextReview = nextReviewSetEvent.NextReview,
                ValuationDateTime = nextReviewSetEvent.EventDateTime,
                AsOfDateTime = nextReviewSetEvent.AuditDateTime,
                LastEventID = nextReviewSetEvent.EventID,
                LastAuditDateTime = nextReviewSetEvent.AuditDateTime
            };
        }

        public Broker Apply(BrokerNotesSetEvent notesSetEvent)
        {
            if (broker is null)
                throw new ArgumentNullException(nameof(broker));

            if (notesSetEvent is null)
                throw new ArgumentNullException(nameof(notesSetEvent));

            return broker with
            {
                Notes = notesSetEvent.Notes,
                ValuationDateTime = notesSetEvent.EventDateTime,
                AsOfDateTime = notesSetEvent.AuditDateTime,
                LastEventID = notesSetEvent.EventID,
                LastAuditDateTime = notesSetEvent.AuditDateTime
            };
        }
    }
}

namespace FolioTrace.Aggregates;

public static class HoldingBuilder
{
    public static Holding Create(HoldingCreatedEvent createdEvent)
    {
        if (createdEvent is null)
            throw new ArgumentNullException(nameof(createdEvent));

        return new Holding(
            createdEvent.HoldingID,
            createdEvent.AccountID,
            createdEvent.InstrumentID,
            createdEvent.HoldingType,
            createdEvent.NominalType,
            createdEvent.Name,
            createdEvent.Active,
            createdEvent.Default,
            createdEvent.EventDateTime,
            createdEvent.AuditDateTime,
            createdEvent.EventID);
    }

    extension(Holding holding)
    {
        public Holding Apply(HoldingModifiedEvent modifiedEvent)
        {
            if (holding is null)
                throw new ArgumentNullException(nameof(holding));

            if (modifiedEvent is null)
                throw new ArgumentNullException(nameof(modifiedEvent));

            return holding with
            {
                NominalType = modifiedEvent.NominalType,
                Name = modifiedEvent.Name,
                Default = modifiedEvent.Default,
                ValuationDateTime = modifiedEvent.EventDateTime,
                AsOfDateTime = modifiedEvent.AuditDateTime,
                LastEventID = modifiedEvent.EventID,
                LastAuditDateTime = modifiedEvent.AuditDateTime
            };
        }

        public Holding Apply(HoldingActiveModifiedEvent activeModifiedEvent)
        {
            if (holding is null)
                throw new ArgumentNullException(nameof(holding));

            if (activeModifiedEvent is null)
                throw new ArgumentNullException(nameof(activeModifiedEvent));

            return holding with
            {
                Active = activeModifiedEvent.Active,
                ValuationDateTime = activeModifiedEvent.EventDateTime,
                AsOfDateTime = activeModifiedEvent.AuditDateTime,
                LastEventID = activeModifiedEvent.EventID,
                LastAuditDateTime = activeModifiedEvent.AuditDateTime
            };
        }
    }
}

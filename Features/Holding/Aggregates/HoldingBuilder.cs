namespace FolioTrace.Aggregates;

public static class HoldingBuilder
{
    public static Holding Create(HoldingCreatedEvent createdEvent)
    {
        if (createdEvent is null)
            throw new ArgumentNullException(nameof(createdEvent));

        return createdEvent.CreateHolding();
    }

    extension(Holding holding)
    {
        public Holding Apply(HoldingModifiedEvent modifiedEvent)
        {
            if (holding is null)
                throw new ArgumentNullException(nameof(holding));

            if (modifiedEvent is null)
                throw new ArgumentNullException(nameof(modifiedEvent));

            return modifiedEvent.Apply(holding);
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

using FolioTrace.Common;

namespace FolioTrace.Aggregates;

[Builder]
public static class FXBuilder
{
    public static FX Create(FXCreatedEvent createdEvent)
    {
        if (createdEvent is null)
            throw new ArgumentNullException(nameof(createdEvent));

        return new FX(
            createdEvent.Pair,
            createdEvent.BaseCurrency,
            createdEvent.QuoteCurrency,
            createdEvent.Pair.DisplayValue,
            createdEvent.Active,
            createdEvent.EventDateTime,
            createdEvent.AuditDateTime,
            createdEvent.EventID,
            createdEvent.AuditDateTime);
    }

    extension(FX fx)
    {
        public FX Apply(FXActiveModifiedEvent modifiedEvent)
        {
            if (fx is null)
                throw new ArgumentNullException(nameof(fx));

            if (modifiedEvent is null)
                throw new ArgumentNullException(nameof(modifiedEvent));

            return fx with
            {
                Active = modifiedEvent.Active,
                ValuationDateTime = modifiedEvent.EventDateTime,
                AsOfDateTime = modifiedEvent.AuditDateTime,
                LastEventID = modifiedEvent.EventID,
                LastAuditDateTime = modifiedEvent.AuditDateTime
            };
        }
    }
}

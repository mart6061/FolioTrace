namespace FolioTrace.Aggregates;

public static class FXRateBuilder
{
    public static FXRate Create(FX fx, FXRateSetEvent setEvent)
    {
        if (fx is null)
            throw new ArgumentNullException(nameof(fx));

        if (setEvent is null)
            throw new ArgumentNullException(nameof(setEvent));

        return new FXRate(
            fx.Pair,
            fx.BaseCurrency,
            fx.QuoteCurrency,
            fx.DisplayPair,
            fx.Active,
            setEvent.Price,
            setEvent.EventDateTime,
            setEvent.AuditDateTime,
            setEvent.EventID,
            setEvent.AuditDateTime);
    }

    extension(FXRate fxRate)
    {
        public FXRate Apply(FX fx)
        {
            if (fxRate is null)
                throw new ArgumentNullException(nameof(fxRate));

            if (fx is null)
                throw new ArgumentNullException(nameof(fx));

            return fxRate with
            {
                BaseCurrency = fx.BaseCurrency,
                QuoteCurrency = fx.QuoteCurrency,
                DisplayPair = fx.DisplayPair,
                Active = fx.Active
            };
        }

        public FXRate Apply(FXRateSetEvent setEvent)
        {
            if (fxRate is null)
                throw new ArgumentNullException(nameof(fxRate));

            if (setEvent is null)
                throw new ArgumentNullException(nameof(setEvent));

            return fxRate with
            {
                Price = setEvent.Price,
                ValuationDateTime = setEvent.EventDateTime,
                AsOfDateTime = setEvent.AuditDateTime,
                LastEventID = setEvent.EventID,
                LastAuditDateTime = setEvent.AuditDateTime
            };
        }
    }
}

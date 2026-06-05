namespace FolioTrace.Aggregates;

public sealed record InstrumentTermsEquity : IInstrumentTerms
{
    public string TermsType => nameof(InstrumentTermsEquity);
}

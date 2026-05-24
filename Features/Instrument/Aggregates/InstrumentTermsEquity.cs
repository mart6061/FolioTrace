namespace FolioTrace.Aggregates;

public sealed record InstrumentTermsEquity : IInstrumentTerms
{
    public string TermsType => nameof(InstrumentTermsEquity);

    public string ToData() => TermsType;

    public string ToDetail() => $"{nameof(InstrumentTermsEquity)}";
}

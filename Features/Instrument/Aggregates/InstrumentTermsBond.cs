using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record InstrumentTermsBond : IInstrumentTerms
{
    public required Money ParAmount { get; init; }

    public required CouponRate CouponRate { get; init; }

    public required CouponFrequency CouponFrequency { get; init; }

    public required DateTime MaturityDate { get; init; }

    public DateTime? IssueDate { get; init; }

    public string? DayCount { get; init; }

    public string TermsType => nameof(InstrumentTermsBond);

    [JsonConstructor]
    [SetsRequiredMembers]
    public InstrumentTermsBond(Money parAmount, CouponRate couponRate, CouponFrequency couponFrequency, DateTime maturityDate, DateTime? issueDate = null, string? dayCount = null)
    {
        ParAmount = parAmount ?? throw new ArgumentNullException(nameof(parAmount));
        CouponRate = couponRate ?? throw new ArgumentNullException(nameof(couponRate));
        CouponFrequency = couponFrequency;
        MaturityDate = maturityDate;
        IssueDate = issueDate;
        DayCount = dayCount;
    }

    public string ToData() => $"{TermsType}|{ParAmount.ToData()}|{CouponRate.ToData()}|{CouponFrequency}|{MaturityDate:O}|{IssueDate:O}|{DayCount}";

    public string ToDetail() => $"{nameof(InstrumentTermsBond)}: (ParAmount: {ParAmount.ToDetail()}, CouponRate: {CouponRate.ToDetail()}, CouponFrequency: {CouponFrequency}, MaturityDate: {MaturityDate:O}, IssueDate: {IssueDate:O}, DayCount: {DayCount})";
}

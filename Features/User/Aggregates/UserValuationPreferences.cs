using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record UserProfileValuationPreferences : IType
{
    public required EventDateTime ValuationDate { get; init; }

    public required bool ShowIncome { get; init; }

    public required bool ShowBook { get; init; }

    [JsonConstructor]
    [SetsRequiredMembers]
    public UserProfileValuationPreferences(EventDateTime valuationDate, bool showIncome, bool showBook)
    {
        if (valuationDate is null)
            throw new ArgumentNullException(nameof(valuationDate));

        ValuationDate = valuationDate;
        ShowIncome = showIncome;
        ShowBook = showBook;
    }

    public string ToData() => $"{ValuationDate.ToData()}|{ShowIncome}|{ShowBook}";

    public string ToDetail() => $"{nameof(UserProfileValuationPreferences)}: (ValuationDate: {ValuationDate.ToDetail()}, ShowIncome: {ShowIncome}, ShowBook: {ShowBook})";
}

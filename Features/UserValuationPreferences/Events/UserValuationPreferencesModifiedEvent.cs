using System.Text.Json.Serialization;
using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record UserValuationPreferencesModifiedEvent : EventBase, IUserValuationPreferencesEvent
{
    public UserValuationDateOption ValuationDateOption { get; init; }

    public ValuationDateBasis ValuationDateBasis { get; init; }

    public bool ShowZeroBalances { get; init; }

    [JsonConstructor]
    private UserValuationPreferencesModifiedEvent()
        : base(null!, null!, null!, null!, string.Empty)
    {
    }

    internal UserValuationPreferencesModifiedEvent(EventID eventID, UserID userID, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, UserValuationDateOption valuationDateOption, ValuationDateBasis valuationDateBasis, bool showZeroBalances)
        : base(eventID, userID, eventDateTime, auditDateTime, reason)
    {
        ValuationDateOption = valuationDateOption;
        ValuationDateBasis = valuationDateBasis;
        ShowZeroBalances = showZeroBalances;
    }

    public override string Type => nameof(UserValuationPreferencesModifiedEvent);

    public override string ToData() => $"{base.ToData()}|{ValuationDateOption}|{ValuationDateBasis}|{ShowZeroBalances}";

    public override string ToDetail() => $"{nameof(UserValuationPreferencesModifiedEvent)}: ({base.ToDetail()}, ValuationDateOption: {ValuationDateOption}, ValuationDateBasis: {ValuationDateBasis}, ShowZeroBalances: {ShowZeroBalances})";
}

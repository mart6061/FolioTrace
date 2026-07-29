using System.Text.Json.Serialization;
using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

[EventClass(EventType = EventClassTypeEnum.Modified, Description = "User Valuation Preferences Modified Event")]
public sealed record UserValuationPreferencesModifiedEvent : EventBase, IUserValuationPreferencesEvent
{
    [EventProperty(Description = "Valuation Date Option")]
    public UserValuationDateOption ValuationDateOption { get; init; }

    [EventProperty(Description = "Start Valuation Date Option")]
    public UserValuationDateOption? StartValuationDateOption { get; init; }

    [EventProperty(Description = "End Valuation Date Option")]
    public UserValuationDateOption? EndValuationDateOption { get; init; }

    [EventProperty(Description = "Holding Date Basis")]
    public HoldingDateBasis HoldingDateBasis { get; init; }

    [EventProperty(Description = "Valuation Price Convention")]
    public ValuationPriceConvention ValuationPriceConvention { get; init; }

    [EventProperty(Description = "Show Zero Balances")]
    public bool ShowZeroBalances { get; init; }

    [JsonConstructor]
    private UserValuationPreferencesModifiedEvent()
        : base(null!, null!, null!, null!, string.Empty)
    {
    }

    internal UserValuationPreferencesModifiedEvent(
        EventID eventID,
        UserID userID,
        EventDateTime eventDateTime,
        AuditDateTime auditDateTime,
        string reason,
        UserValuationDateOption startValuationDateOption,
        UserValuationDateOption endValuationDateOption,
        HoldingDateBasis holdingDateBasis,
        ValuationPriceConvention valuationPriceConvention,
        bool showZeroBalances)
        : base(eventID, userID, eventDateTime, auditDateTime, reason)
    {
        ValuationDateOption = endValuationDateOption;
        StartValuationDateOption = startValuationDateOption;
        EndValuationDateOption = endValuationDateOption;
        HoldingDateBasis = holdingDateBasis;
        ValuationPriceConvention = valuationPriceConvention;
        ShowZeroBalances = showZeroBalances;
    }

    public override string Type => nameof(UserValuationPreferencesModifiedEvent);
}

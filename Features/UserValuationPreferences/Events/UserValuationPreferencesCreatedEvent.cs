using System.Text.Json.Serialization;
using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

[EventClass(EventType = EventClassTypeEnum.Created, Description = "User Valuation Preferences Created Event")]
public sealed record UserValuationPreferencesCreatedEvent : EventBase, IUserValuationPreferencesEvent
{
    [EventProperty(Description = "Valuation Date Option")]
    public UserValuationDateOption ValuationDateOption { get; init; }

    [EventProperty(Description = "Holding Date Basis")]
    public HoldingDateBasis HoldingDateBasis { get; init; }

    [EventProperty(Description = "Show Zero Balances")]
    public bool ShowZeroBalances { get; init; }

    [JsonConstructor]
    private UserValuationPreferencesCreatedEvent()
        : base(null!, null!, null!, null!, string.Empty)
    {
    }

    internal UserValuationPreferencesCreatedEvent(EventID eventID, UserID userID, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, UserValuationDateOption valuationDateOption, HoldingDateBasis holdingDateBasis, bool showZeroBalances)
        : base(eventID, userID, eventDateTime, auditDateTime, reason)
    {
        ValuationDateOption = valuationDateOption;
        HoldingDateBasis = holdingDateBasis;
        ShowZeroBalances = showZeroBalances;
    }

    public override string Type => nameof(UserValuationPreferencesCreatedEvent);
}

using System.Text.Json.Serialization;
using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record UserValuationPreferencesCreatedEvent : EventBase, IUserValuationPreferencesEvent
{
    public UserValuationDateOption ValuationDateOption { get; init; }

    public HoldingDateBasis HoldingDateBasis { get; init; }

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

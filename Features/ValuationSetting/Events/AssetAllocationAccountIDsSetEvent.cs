using System.Text.Json.Serialization;
using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

[EventClass(EventType = EventClassTypeEnum.Modified, Description = "Asset Allocation Account IDs Set Event")]
public sealed record AssetAllocationAccountIDsSetEvent : EventBase, IValuationSettingEvent
{
    [EventProperty(Description = "Asset Allocation ID")]
    public AssetAllocationID AssetAllocationID { get; init; } = null!;

    [EventProperty(Description = "Account IDs")]
    public List<AccountID> AccountIDs { get; init; } = [];

    [JsonConstructor]
    private AssetAllocationAccountIDsSetEvent()
        : base(null!, null!, null!, null!, string.Empty)
    {
    }

    internal AssetAllocationAccountIDsSetEvent(EventID eventId, UserID userId, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, AssetAllocationID assetAllocationID, List<AccountID> accountIDs)
        : base(eventId, userId, eventDateTime, auditDateTime, reason)
    {
        AssetAllocationID = assetAllocationID;
        AccountIDs = ValuationSettingBuilder.CloneAccountIDs(accountIDs);
    }

    public override string Type => nameof(AssetAllocationAccountIDsSetEvent);
}

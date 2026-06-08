using System.Text.Json.Serialization;
using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record AssetAllocationActiveSetEvent : EventBase, IValuationSettingEvent
{
    public AssetAllocationID AssetAllocationID { get; init; } = null!;

    public bool Active { get; init; }

    [JsonConstructor]
    private AssetAllocationActiveSetEvent()
        : base(null!, null!, null!, null!, string.Empty)
    {
    }

    internal AssetAllocationActiveSetEvent(EventID eventId, UserID userId, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, AssetAllocationID assetAllocationID, bool active)
        : base(eventId, userId, eventDateTime, auditDateTime, reason)
    {
        AssetAllocationID = assetAllocationID;
        Active = active;
    }

    public override string Type => nameof(AssetAllocationActiveSetEvent);
}

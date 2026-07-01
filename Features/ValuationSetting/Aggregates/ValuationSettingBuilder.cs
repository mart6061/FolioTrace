using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

[Builder]
public static class ValuationSettingBuilder
{
    public static ValuationSetting Create(AssetAllocationCreatedEvent createdEvent)
    {
        if (createdEvent is null)
            throw new ArgumentNullException(nameof(createdEvent));

        return new ValuationSetting(
            createdEvent.AssetAllocationID,
            createdEvent.Name,
            CloneAccountIDs(createdEvent.AccountIDs),
            createdEvent.Active,
            createdEvent.RootNodeID,
            CloneNodes(createdEvent.Nodes),
            createdEvent.AuditDateTime,
            createdEvent.EventID);
    }

    public static ValuationSetting Apply(ValuationSetting setting, AssetAllocationModifiedEvent modifiedEvent)
    {
        if (setting is null)
            throw new ArgumentNullException(nameof(setting));

        if (modifiedEvent is null)
            throw new ArgumentNullException(nameof(modifiedEvent));

        return setting with
        {
            Name = modifiedEvent.Name,
            RootNodeID = modifiedEvent.RootNodeID,
            Nodes = CloneNodes(modifiedEvent.Nodes),
            AsOfDateTime = modifiedEvent.AuditDateTime,
            LastEventID = modifiedEvent.EventID,
            LastAuditDateTime = modifiedEvent.AuditDateTime
        };
    }

    public static ValuationSetting Apply(ValuationSetting setting, AssetAllocationAccountIDsSetEvent accountIDsSetEvent)
    {
        if (setting is null)
            throw new ArgumentNullException(nameof(setting));

        if (accountIDsSetEvent is null)
            throw new ArgumentNullException(nameof(accountIDsSetEvent));

        return setting with
        {
            AccountIDs = CloneAccountIDs(accountIDsSetEvent.AccountIDs),
            AsOfDateTime = accountIDsSetEvent.AuditDateTime,
            LastEventID = accountIDsSetEvent.EventID,
            LastAuditDateTime = accountIDsSetEvent.AuditDateTime
        };
    }

    public static ValuationSetting Apply(ValuationSetting setting, AssetAllocationActiveSetEvent activeSetEvent)
    {
        if (setting is null)
            throw new ArgumentNullException(nameof(setting));

        if (activeSetEvent is null)
            throw new ArgumentNullException(nameof(activeSetEvent));

        return setting with
        {
            Active = activeSetEvent.Active,
            AsOfDateTime = activeSetEvent.AuditDateTime,
            LastEventID = activeSetEvent.EventID,
            LastAuditDateTime = activeSetEvent.AuditDateTime
        };
    }

    internal static List<AssetAllocationNode> CloneNodes(IEnumerable<AssetAllocationNode> nodes) =>
        nodes
            .Select(node => node with
            {
                Nodes = node.Nodes.ToList(),
                AccountSettings = node.AccountSettings.ToList()
            })
            .ToList();

    internal static List<FolioTrace.Types.AccountID> CloneAccountIDs(IEnumerable<FolioTrace.Types.AccountID> accountIDs) =>
        accountIDs.ToList();

    internal static EventDateTime DateOnly(EventDateTime eventDateTime)
    {
        var date = eventDateTime.Value.Date;
        return date == default ? eventDateTime : new EventDateTime(date);
    }
}

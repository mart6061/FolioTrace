using FolioTrace.Types;

namespace FolioTrace.Aggregates;

internal static class AssetAllocationEventValidation
{
    public static List<string> ValidateBase(EventID? eventId, UserID? userId, EventDateTime? eventDateTime, AuditDateTime? auditDateTime, string? reason, AssetAllocationID? assetAllocationID)
    {
        var messages = new List<string>();

        if (eventId is null)
            messages.Add("EventID is required.");

        if (userId is null)
            messages.Add("UserID is required.");

        if (eventDateTime is null)
            messages.Add("EventDateTime is required.");

        if (auditDateTime is null)
            messages.Add("AuditDateTime is required.");

        if (string.IsNullOrWhiteSpace(reason))
            messages.Add("Reason is required.");

        if (assetAllocationID is null)
            messages.Add("AssetAllocationID is required.");

        return messages;
    }

    public static void ValidateDefinition(List<string> messages, string? name, IReadOnlyList<AccountID>? accountIDs, NodeID? rootNodeID, IReadOnlyList<AssetAllocationNode>? nodes)
    {
        if (string.IsNullOrWhiteSpace(name))
            messages.Add("Name is required.");

        ValidateAccountIDs(messages, accountIDs);
        ValidateNodes(messages, accountIDs ?? [], rootNodeID, nodes);
    }

    public static void ValidateAccountIDs(List<string> messages, IReadOnlyList<AccountID>? accountIDs)
    {
        if (accountIDs is null)
        {
            messages.Add("AccountIDs is required.");
            return;
        }

        if (accountIDs.Any(accountID => accountID is null))
            messages.Add("AccountIDs must not contain null values.");

        var duplicateAccountIDs = accountIDs
            .Where(accountID => accountID is not null)
            .GroupBy(accountID => accountID.Value)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToList();

        foreach (var duplicateAccountID in duplicateAccountIDs)
            messages.Add($"AccountID '{duplicateAccountID}' is duplicated.");
    }

    public static void ValidateExistingAllocation(List<string> messages, AssetAllocationID assetAllocationID, ValuationSettings? valuationSettings)
    {
        if (valuationSettings is null || !valuationSettings.Items.Any(item => item.AssetAllocationID == assetAllocationID))
            messages.Add($"No matching asset allocation found for AssetAllocationID '{assetAllocationID}'.");
    }

    public static void ValidateCreatedAllocation(List<string> messages, AssetAllocationID assetAllocationID, ValuationSettings? valuationSettings)
    {
        if (valuationSettings?.Items.Any(item => item.AssetAllocationID == assetAllocationID) == true)
            messages.Add($"Asset allocation already exists for AssetAllocationID '{assetAllocationID}'.");
    }

    private static void ValidateNodes(List<string> messages, IReadOnlyList<AccountID> accountIDs, NodeID? rootNodeID, IReadOnlyList<AssetAllocationNode>? nodes)
    {
        if (rootNodeID is null)
            messages.Add("RootNodeID is required.");

        if (nodes is null)
        {
            messages.Add("Nodes is required.");
            return;
        }

        if (nodes.Count == 0)
        {
            messages.Add("Nodes must contain at least the root node.");
            return;
        }

        if (nodes.Any(node => node is null))
        {
            messages.Add("Nodes must not contain null values.");
            return;
        }

        foreach (var node in nodes)
        {
            if (node.NodeID is null)
                messages.Add("Every node requires a NodeID.");

            if (string.IsNullOrWhiteSpace(node.Name))
                messages.Add("Every node requires a Name.");

            if (node.Nodes is null)
                messages.Add($"Node '{node.NodeID}' requires a child Nodes list.");

            if (node.AccountSettings is null)
                messages.Add($"Node '{node.NodeID}' requires an AccountSettings list.");
        }

        var nodeIDs = nodes
            .Where(node => node.NodeID is not null)
            .Select(node => node.NodeID.Value)
            .ToList();

        foreach (var duplicateNodeID in nodeIDs.GroupBy(id => id).Where(group => group.Count() > 1).Select(group => group.Key))
            messages.Add($"NodeID '{duplicateNodeID}' is duplicated.");

        if (rootNodeID is not null && !nodeIDs.Contains(rootNodeID.Value))
            messages.Add($"RootNodeID '{rootNodeID}' does not exist in Nodes.");

        var nodeIDSet = nodeIDs.ToHashSet();
        var accountIDSet = accountIDs
            .Where(accountID => accountID is not null)
            .Select(accountID => accountID.Value)
            .ToHashSet();

        foreach (var node in nodes.Where(node => node.NodeID is not null))
        {
            foreach (var childNodeID in node.Nodes ?? [])
            {
                if (childNodeID is null)
                {
                    messages.Add($"Node '{node.NodeID}' has a null child NodeID.");
                    continue;
                }

                if (childNodeID.Value == node.NodeID.Value)
                    messages.Add($"Node '{node.NodeID}' cannot contain itself as a child.");

                if (!nodeIDSet.Contains(childNodeID.Value))
                    messages.Add($"Node '{node.NodeID}' references missing child NodeID '{childNodeID}'.");
            }

            foreach (var setting in node.AccountSettings ?? [])
            {
                if (setting.AccountID is null)
                {
                    messages.Add($"Node '{node.NodeID}' has an account setting without AccountID.");
                    continue;
                }

                if (accountIDSet.Count > 0 && !accountIDSet.Contains(setting.AccountID.Value))
                    messages.Add($"Node '{node.NodeID}' has target settings for AccountID '{setting.AccountID}' which is not assigned to the allocation.");

                if (setting.TargetWeightMin > setting.TargetWeightMax)
                    messages.Add($"Node '{node.NodeID}' has TargetWeightMin greater than TargetWeightMax for AccountID '{setting.AccountID}'.");
            }
        }
    }
}

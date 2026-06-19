using FolioTrace.Types;

namespace FolioTrace.Aggregates;

internal static class AssetAllocationEventValidation
{
    public static List<string> ValidateBase(EventID? eventId, UserID? userId, AuditDateTime? auditDateTime, AssetAllocationID? assetAllocationID)
    {
        var messages = new List<string>();

        if (eventId is null)
            messages.Add("EventID is required.");

        if (userId is null)
            messages.Add("UserID is required.");

        if (auditDateTime is null)
            messages.Add("AuditDateTime is required.");

        if (assetAllocationID is null)
            messages.Add("AssetAllocationID is required.");

        return messages;
    }

    public static List<string> ValidateBase(EventID? eventId, UserID? userId, EventDateTime? eventDateTime, AuditDateTime? auditDateTime, string? reason, AssetAllocationID? assetAllocationID)
    {
        var messages = ValidateBase(eventId, userId, auditDateTime, assetAllocationID);

        if (eventDateTime is null)
            messages.Add("EventDateTime is required.");

        if (string.IsNullOrWhiteSpace(reason))
            messages.Add("Reason is required.");

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
            messages.Add("Nodes must contain at least one node.");

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

        if (rootNodeID is not null && nodeIDs.Contains(rootNodeID.Value))
            messages.Add($"RootNodeID '{rootNodeID}' must not exist in Nodes.");

        var nodeIDSet = nodeIDs.ToHashSet();
        var nodeByID = nodes
            .Where(node => node.NodeID is not null)
            .GroupBy(node => node.NodeID.Value)
            .ToDictionary(group => group.Key, group => group.First());
        var accountIDSet = accountIDs
            .Where(accountID => accountID is not null)
            .Select(accountID => accountID.Value)
            .ToHashSet();

        foreach (var node in nodes.Where(node => node.NodeID is not null))
        {
            if (!string.IsNullOrWhiteSpace(node.Colour) && !IsValidHexColour(node.Colour))
                messages.Add($"Node '{node.NodeID}' has invalid Colour '{node.Colour}'.");

            foreach (var duplicateChildNodeID in (node.Nodes ?? []).Where(childNodeID => childNodeID is not null).GroupBy(childNodeID => childNodeID.Value).Where(group => group.Count() > 1).Select(group => group.Key))
                messages.Add($"Node '{node.NodeID}' references child NodeID '{duplicateChildNodeID}' more than once.");

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

                if (setting.TargetWeightMin.HasValue && setting.TargetWeightMax.HasValue && setting.TargetWeightMin > setting.TargetWeightMax)
                    messages.Add($"Node '{node.NodeID}' has TargetWeightMin greater than TargetWeightMax for AccountID '{setting.AccountID}'.");
            }
        }

        ValidateRootedTree(messages, rootNodeID, nodes, nodeByID);
    }

    private static void ValidateRootedTree(List<string> messages, NodeID? rootNodeID, IReadOnlyList<AssetAllocationNode> nodes, Dictionary<Guid, AssetAllocationNode> nodeByID)
    {
        if (rootNodeID is null || nodeByID.Count == 0)
            return;

        var parentIDsByChildID = new Dictionary<Guid, List<Guid>>();

        foreach (var node in nodeByID.Values)
        {
            foreach (var childNodeID in node.Nodes ?? [])
            {
                if (childNodeID is null || !nodeByID.ContainsKey(childNodeID.Value))
                    continue;

                if (!parentIDsByChildID.TryGetValue(childNodeID.Value, out var parentIDs))
                {
                    parentIDs = [];
                    parentIDsByChildID[childNodeID.Value] = parentIDs;
                }

                parentIDs.Add(node.NodeID.Value);
            }
        }

        if (parentIDsByChildID.ContainsKey(rootNodeID.Value))
            messages.Add($"RootNodeID '{rootNodeID}' cannot be referenced as a child node.");

        foreach (var childParentPair in parentIDsByChildID.Where(pair => pair.Value.Distinct().Count() > 1))
            messages.Add($"NodeID '{childParentPair.Key}' has multiple parents.");

        var topLevelNodeIDs = nodes
            .Where(node => node.NodeID is not null && !parentIDsByChildID.ContainsKey(node.NodeID.Value))
            .Select(node => node.NodeID.Value)
            .ToList();

        if (topLevelNodeIDs.Count == 0)
            messages.Add("Nodes must contain at least one top-level node.");

        var visited = new HashSet<Guid>();
        var visiting = new HashSet<Guid>();
        foreach (var topLevelNodeID in topLevelNodeIDs)
            VisitNode(topLevelNodeID, nodeByID, visited, visiting, messages);

        foreach (var nodeID in nodeByID.Keys.Where(nodeID => !visited.Contains(nodeID)).ToList())
        {
            messages.Add($"NodeID '{nodeID}' is not reachable from a top-level node.");
            VisitNode(nodeID, nodeByID, visited, visiting, messages);
        }
    }

    private static void VisitNode(Guid nodeID, Dictionary<Guid, AssetAllocationNode> nodeByID, HashSet<Guid> visited, HashSet<Guid> visiting, List<string> messages)
    {
        if (visited.Contains(nodeID))
            return;

        if (!visiting.Add(nodeID))
        {
            messages.Add($"NodeID '{nodeID}' creates a cycle in Nodes.");
            return;
        }

        if (!nodeByID.TryGetValue(nodeID, out var node))
        {
            visiting.Remove(nodeID);
            return;
        }

        foreach (var childNodeID in node.Nodes ?? [])
        {
            if (childNodeID is not null && nodeByID.ContainsKey(childNodeID.Value))
                VisitNode(childNodeID.Value, nodeByID, visited, visiting, messages);
        }

        visiting.Remove(nodeID);
        visited.Add(nodeID);
    }

    private static bool IsValidHexColour(string colour) =>
        colour.Length == 7
        && colour[0] == '#'
        && colour.Skip(1).All(Uri.IsHexDigit);
}

using FolioTrace.Types;

namespace FolioTrace.Aggregates;

internal static class AssetAllocationNodeNormalizer
{
    private const string SpecialNodeName = "Unallocated";
    private const string SpecialNodeColour = "#dc2626";

    public static List<AssetAllocationNode> Normalise(NodeID rootNodeID, string rootName, List<AssetAllocationNode>? nodes)
    {
        var sourceNodes = CloneNodes(nodes is { Count: > 0 }
            ? nodes
            : []);
        var legacyRootNode = sourceNodes.FirstOrDefault(node => node.NodeID == rootNodeID);
        var normalisedNodes = sourceNodes
            .Where(node => node.NodeID != rootNodeID)
            .ToList();

        var specialNode = normalisedNodes.FirstOrDefault(IsSpecialNode);
        if (specialNode is null)
        {
            specialNode = new AssetAllocationNode(NodeIDBuilder.Create(), [], SpecialNodeName, false, false, [], SpecialNodeColour);
            normalisedNodes.Add(specialNode);
        }

        var specialChildNodeIDs = (specialNode.Nodes ?? [])
            .Where(nodeID => nodeID is not null && nodeID != rootNodeID && nodeID != specialNode.NodeID)
            .ToList();
        var legacyTopLevelNodeIDs = (legacyRootNode?.Nodes ?? [])
            .Where(nodeID => nodeID is not null && nodeID != rootNodeID && nodeID != specialNode.NodeID)
            .ToList();

        specialNode = specialNode with
        {
            AccountSettings = [],
            Colour = SpecialNodeColour,
            Hidden = false,
            Name = SpecialNodeName,
            Nodes = [],
            Subtotal = false
        };

        for (var index = 0; index < normalisedNodes.Count; index++)
        {
            var node = normalisedNodes[index];
            var childNodeIDs = (node.Nodes ?? [])
                .Where(nodeID => nodeID is not null && nodeID != rootNodeID && nodeID != specialNode.NodeID)
                .ToList();

            normalisedNodes[index] = node.NodeID == specialNode.NodeID
                ? specialNode
                : node with { Nodes = childNodeIDs };
        }

        return OrderNodes(normalisedNodes, specialNode.NodeID, [.. specialChildNodeIDs, .. legacyTopLevelNodeIDs]);
    }

    private static List<AssetAllocationNode> OrderNodes(IReadOnlyList<AssetAllocationNode> nodes, NodeID specialNodeID, IReadOnlyList<NodeID> legacyTopLevelNodeIDs)
    {
        var childNodeIDs = nodes
            .SelectMany(node => node.Nodes ?? [])
            .Where(nodeID => nodeID is not null)
            .Select(nodeID => nodeID.Value)
            .ToHashSet();
        var nodeByID = nodes
            .Where(node => node.NodeID is not null)
            .GroupBy(node => node.NodeID.Value)
            .ToDictionary(group => group.Key, group => group.First());
        var orderedNodeIDs = new List<Guid> { specialNodeID.Value };

        foreach (var topLevelNodeID in legacyTopLevelNodeIDs)
        {
            if (topLevelNodeID is not null)
                orderedNodeIDs.Add(topLevelNodeID.Value);
        }

        foreach (var node in nodes)
        {
            if (node.NodeID is not null && !childNodeIDs.Contains(node.NodeID.Value))
                orderedNodeIDs.Add(node.NodeID.Value);
        }

        var result = new List<AssetAllocationNode>();
        var addedNodeIDs = new HashSet<Guid>();

        foreach (var nodeID in orderedNodeIDs)
        {
            if (addedNodeIDs.Add(nodeID) && nodeByID.TryGetValue(nodeID, out var node))
                result.Add(node);
        }

        foreach (var node in nodes)
        {
            if (node.NodeID is not null && addedNodeIDs.Add(node.NodeID.Value))
                result.Add(node);
        }

        return result;
    }

    private static List<AssetAllocationNode> CloneNodes(IEnumerable<AssetAllocationNode> nodes) =>
        nodes
            .Where(node => node is not null)
            .Select(node => node with
            {
                AccountSettings = node.AccountSettings?.ToList() ?? [],
                Nodes = node.Nodes?.ToList() ?? []
            })
            .ToList();

    private static bool IsSpecialNode(AssetAllocationNode node) =>
        string.Equals(node.Name?.Trim(), SpecialNodeName, StringComparison.OrdinalIgnoreCase);

}

using FolioTrace.Types;

namespace FolioTrace.Aggregates;

internal static class AssetAllocationNodeNormalizer
{
    private const string SpecialNodeName = "Unallocated";
    private const string SpecialNodeColour = "#dc2626";

    public static List<AssetAllocationNode> Normalise(NodeID rootNodeID, string rootName, List<AssetAllocationNode>? nodes)
    {
        var normalisedNodes = CloneNodes(nodes is { Count: > 0 }
            ? nodes
            :
            [
                new AssetAllocationNode(rootNodeID, [], rootName, false, true, [], "#0f766e")
            ]);
        var rootNode = normalisedNodes.FirstOrDefault(node => node.NodeID == rootNodeID);

        if (rootNode is null)
            return normalisedNodes;

        var specialNode = normalisedNodes.FirstOrDefault(IsSpecialNode);
        if (specialNode is null)
        {
            specialNode = new AssetAllocationNode(NodeIDBuilder.Create(), [], SpecialNodeName, false, false, [], SpecialNodeColour);
            normalisedNodes.Add(specialNode);
        }

        var specialChildNodeIDs = (specialNode.Nodes ?? [])
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
            var childNodeIDs = (node.Nodes ?? []).Where(nodeID => nodeID is not null && nodeID != specialNode.NodeID).ToList();

            normalisedNodes[index] = node.NodeID == rootNodeID
                ? node with { Name = rootName, Nodes = [specialNode.NodeID, .. specialChildNodeIDs, .. childNodeIDs] }
                : node.NodeID == specialNode.NodeID
                    ? specialNode
                    : node with { Nodes = childNodeIDs };
        }

        return normalisedNodes;
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

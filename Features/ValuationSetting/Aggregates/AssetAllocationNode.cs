using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record AssetAllocationNode(
    NodeID NodeID,
    List<NodeID> Nodes,
    string Name,
    bool Subtotal,
    bool Hidden,
    List<AssetAllocationNodeAccountSetting> AccountSettings,
    string? Colour = null);

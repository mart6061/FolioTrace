using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public interface IAssetAllocationMappingEvent : IEventBase
{
    AssetAllocationID AssetAllocationID { get; }
    HoldingID HoldingID { get; }
    NodeID NodeID { get; }
}

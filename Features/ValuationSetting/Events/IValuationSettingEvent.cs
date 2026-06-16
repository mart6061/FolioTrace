using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public interface IValuationSettingEvent : IConfigEventBase
{
    AssetAllocationID AssetAllocationID { get; }
}

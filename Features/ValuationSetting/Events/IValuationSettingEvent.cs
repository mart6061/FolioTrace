using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public interface IValuationSettingEvent : IEventBase
{
    AssetAllocationID AssetAllocationID { get; }
}

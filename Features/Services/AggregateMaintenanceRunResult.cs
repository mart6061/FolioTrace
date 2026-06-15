using FolioTrace.Aggregates;
using FolioTrace.Types;

namespace Services;

internal sealed class AggregateMaintenanceRunResult
{
    public int ScannedAggregates { get; set; }

    public int MissingAggregates { get; set; }

    public int FixedAggregates { get; set; }

    public int FailedAggregates { get; set; }

    public List<string> Errors { get; } = [];
}

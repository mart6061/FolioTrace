using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record HoldingPositionFilter(HoldingID? HoldingID, AccountID? AccountID, InstrumentID? InstrumentID, bool IncludeExcluded, bool IncludeZero)
{
    public static HoldingPositionFilter Default { get; } = new(null, null, null, false, false);
}

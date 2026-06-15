using System.Text.Json.Serialization;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record ReportValuationColumn(
    ReportValuationColumnKey ColumnKey,
    int DisplayOrder);

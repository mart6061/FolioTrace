using FolioTrace;
using FolioTrace.Aggregates;
using FolioTrace.Types;
using Repository;

namespace Services;

public sealed record ReportConfigServiceDiagnostics(int CacheEntries, int ReportCount, long EstimatedBytes);

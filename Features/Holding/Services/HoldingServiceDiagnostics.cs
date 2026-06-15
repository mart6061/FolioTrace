using FolioTrace;
using FolioTrace.Aggregates;
using FolioTrace.Types;
using Repository;

namespace Services;

public sealed record HoldingServiceDiagnostics(int CacheEntryCount, int HoldingCount, long EstimatedMemoryBytes);

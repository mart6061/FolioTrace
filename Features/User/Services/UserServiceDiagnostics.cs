using FolioTrace;
using FolioTrace.Aggregates;
using FolioTrace.Types;
using Repository;

namespace Services;

public sealed record UserServiceDiagnostics(int CacheEntryCount, int UserCount, long EstimatedMemoryBytes);

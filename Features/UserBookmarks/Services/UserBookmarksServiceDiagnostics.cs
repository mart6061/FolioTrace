using FolioTrace;
using FolioTrace.Aggregates;
using FolioTrace.Types;
using Repository;

namespace Services;

public sealed record UserBookmarksServiceDiagnostics(int CacheEntryCount, long EstimatedMemoryBytes);

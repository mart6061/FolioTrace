using FolioTrace;
using FolioTrace.Aggregates;
using FolioTrace.Types;
using Repository;

namespace Services;

public sealed record UserValuationPreferencesServiceDiagnostics(int CacheEntryCount, long EstimatedMemoryBytes);

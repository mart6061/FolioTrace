using FolioTrace;
using FolioTrace.Aggregates;
using FolioTrace.Types;
using Repository;

namespace Services;

public sealed record UserMenuPreferencesServiceDiagnostics(int CacheEntryCount, long EstimatedMemoryBytes);

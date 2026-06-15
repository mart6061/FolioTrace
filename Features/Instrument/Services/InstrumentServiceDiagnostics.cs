using FolioTrace;
using FolioTrace.Aggregates;
using FolioTrace.Types;
using Repository;

namespace Services;

public sealed record InstrumentServiceDiagnostics(int CacheEntryCount, int InstrumentCount, long EstimatedMemoryBytes);

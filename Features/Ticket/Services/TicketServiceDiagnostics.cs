using FolioTrace;
using FolioTrace.Aggregates;
using FolioTrace.Types;
using Repository;

namespace Services;

public sealed record TicketServiceDiagnostics(int CacheEntryCount, int TicketCount, long EstimatedMemoryBytes);

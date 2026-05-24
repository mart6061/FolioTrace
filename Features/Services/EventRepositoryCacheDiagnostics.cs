namespace Repository;

public sealed record EventRepositoryCacheDiagnostics(
    bool IsLoaded,
    int StreamCount,
    int EventCount,
    long EstimatedMemoryBytes,
    int UnprocessedEventCount,
    IReadOnlyList<UnprocessedEventDiagnostic> RecentUnprocessedEvents);

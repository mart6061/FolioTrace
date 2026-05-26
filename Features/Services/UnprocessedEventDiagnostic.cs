namespace Repository;

public sealed record UnprocessedEventDiagnostic(
    Guid? StreamId,
    Guid? EventId,
    string EventType,
    string Reason,
    DateTime RecordedAtUtc);

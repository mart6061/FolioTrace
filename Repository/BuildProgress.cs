namespace Repository;

public sealed record BuildProgress(
    string Stage,
    string Message,
    int CompletedSteps,
    int TotalSteps,
    int CompletedEvents,
    int TotalEvents);

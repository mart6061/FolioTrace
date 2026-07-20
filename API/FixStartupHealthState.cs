namespace API;

public enum FixStartupStatus
{
    Starting,
    Ready,
    Failed
}

public sealed record FixStartupHealthSnapshot(
    FixStartupStatus Status,
    string? FailureMessage,
    DateTime? FailedAtUtc);

public sealed class FixStartupHealthState
{
    private readonly Lock sync = new();
    private FixStartupHealthSnapshot snapshot = new(FixStartupStatus.Starting, null, null);

    public FixStartupHealthSnapshot Snapshot
    {
        get
        {
            lock (sync)
                return snapshot;
        }
    }

    public void MarkReady()
    {
        lock (sync)
            snapshot = new(FixStartupStatus.Ready, null, null);
    }

    public void MarkFailed(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        lock (sync)
            snapshot = new(FixStartupStatus.Failed, exception.Message, DateTime.UtcNow);
    }
}

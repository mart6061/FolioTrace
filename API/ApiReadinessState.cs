namespace API;

public sealed class ApiReadinessState
{
    private readonly object sync = new();
    private TaskCompletionSource readiness = CreateReadinessSource();
    private volatile bool ready;

    public bool Ready => ready;

    public void MarkReady()
    {
        lock (sync)
        {
            ready = true;
            readiness.TrySetResult();
        }
    }

    public void MarkNotReady()
    {
        lock (sync)
        {
            if (!ready)
                return;

            ready = false;
            readiness = CreateReadinessSource();
        }
    }

    public Task WaitUntilReadyAsync(CancellationToken cancellationToken)
    {
        lock (sync)
            return readiness.Task.WaitAsync(cancellationToken);
    }

    private static TaskCompletionSource CreateReadinessSource() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);
}

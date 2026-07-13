namespace API;

public sealed class ApiReadinessState
{
    private readonly TaskCompletionSource readiness = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private volatile bool ready;

    public bool Ready => ready;

    public void MarkReady()
    {
        ready = true;
        readiness.TrySetResult();
    }

    public Task WaitUntilReadyAsync(CancellationToken cancellationToken) =>
        readiness.Task.WaitAsync(cancellationToken);
}

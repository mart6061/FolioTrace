using Repository;

namespace API;

public sealed record RequestTraceLogScope(Guid RequestId, string Source);

public static class RequestTraceLogContext
{
    private static readonly AsyncLocal<RequestTraceLogScope?> CurrentScope = new();

    public static RequestTraceLogScope? Current => CurrentScope.Value;

    public static IDisposable Begin(Guid requestId, string source)
    {
        var previous = CurrentScope.Value;
        CurrentScope.Value = new RequestTraceLogScope(requestId, source);

        return new Scope(() => CurrentScope.Value = previous);
    }

    private sealed class Scope(Action dispose) : IDisposable
    {
        private bool disposed;

        public void Dispose()
        {
            if (disposed)
                return;

            disposed = true;
            dispose();
        }
    }
}

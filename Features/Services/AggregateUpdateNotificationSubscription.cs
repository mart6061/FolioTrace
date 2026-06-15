using System.Collections.Concurrent;
using System.Threading.Channels;
using FolioTrace.Aggregates;
using FolioTrace.Common;

namespace Services;

public sealed class AggregateUpdateNotificationSubscription(
    ChannelReader<object> reader,
    Action unsubscribe) : IAsyncDisposable
{
    public ChannelReader<object> Reader { get; } = reader;

    public ValueTask DisposeAsync()
    {
        unsubscribe();
        return ValueTask.CompletedTask;
    }
}

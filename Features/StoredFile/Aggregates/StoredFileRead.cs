namespace FolioTrace.Aggregates;

public sealed class StoredFileRead(
    StoredFileMetadata metadata,
    Stream content,
    IAsyncDisposable? owner = null) : IAsyncDisposable
{
    public StoredFileMetadata Metadata { get; } = metadata;
    public Stream Content { get; } = content;

    public async ValueTask DisposeAsync()
    {
        await Content.DisposeAsync();
        if (owner is not null)
            await owner.DisposeAsync();
    }
}

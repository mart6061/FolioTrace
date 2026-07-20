using API.TradeFiles;
using FolioTrace.Aggregates;
using Microsoft.AspNetCore.Http;
using Repository;

namespace Test;

public sealed class StoredFileHttpResultTests
{
    private static readonly Guid FileID = Guid.Parse("019f5c50-0acd-7fc3-86e0-7bf80fe76950");
    private static readonly byte[] Content = "0123456789"u8.ToArray();

    [Fact]
    public async Task ExecuteAsync_StreamsTheCompleteFileAndAdvertisesRanges()
    {
        var context = CreateContext();

        await new StoredFileHttpResult(FileID, new FakeStoredFileRepository(Content)).ExecuteAsync(context);

        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
        Assert.Equal("bytes", context.Response.Headers.AcceptRanges);
        Assert.Equal(Content.Length, context.Response.ContentLength);
        Assert.Equal(Content, ((MemoryStream)context.Response.Body).ToArray());
    }

    [Theory]
    [InlineData("bytes=2-5", "2345", "bytes 2-5/10")]
    [InlineData("bytes=7-", "789", "bytes 7-9/10")]
    [InlineData("bytes=-3", "789", "bytes 7-9/10")]
    public async Task ExecuteAsync_StreamsSingleByteRanges(string range, string expected, string contentRange)
    {
        var context = CreateContext();
        context.Request.Headers.Range = range;

        await new StoredFileHttpResult(FileID, new FakeStoredFileRepository(Content)).ExecuteAsync(context);

        Assert.Equal(StatusCodes.Status206PartialContent, context.Response.StatusCode);
        Assert.Equal(contentRange, context.Response.Headers.ContentRange);
        Assert.Equal(expected, System.Text.Encoding.UTF8.GetString(((MemoryStream)context.Response.Body).ToArray()));
    }

    [Theory]
    [InlineData("bytes=20-30")]
    [InlineData("bytes=8-2")]
    [InlineData("bytes=0-1,4-5")]
    public async Task ExecuteAsync_Returns416ForInvalidOrMultipleRanges(string range)
    {
        var context = CreateContext();
        context.Request.Headers.Range = range;

        await new StoredFileHttpResult(FileID, new FakeStoredFileRepository(Content)).ExecuteAsync(context);

        Assert.Equal(StatusCodes.Status416RangeNotSatisfiable, context.Response.StatusCode);
        Assert.Equal("bytes */10", context.Response.Headers.ContentRange);
        Assert.Empty(((MemoryStream)context.Response.Body).ToArray());
    }

    private static DefaultHttpContext CreateContext()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        return context;
    }

    private sealed class FakeStoredFileRepository(byte[] content) : IStoredFileRepository
    {
        private readonly StoredFileMetadata metadata = new(FileID, "trades.xlsx", "application/test", content.LongLength, "SHA");

        public Task InitializeAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<StoredFileMetadata?> GetMetadataAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult<StoredFileMetadata?>(id == FileID ? metadata : null);

        public Task<StoredFileRead?> OpenReadAsync(Guid id, long offset = 0, long? length = null, CancellationToken cancellationToken = default)
        {
            if (id != FileID)
                return Task.FromResult<StoredFileRead?>(null);

            var selectedLength = checked((int)(length ?? (content.LongLength - offset)));
            var bytes = content.AsSpan(checked((int)offset), selectedLength).ToArray();
            return Task.FromResult<StoredFileRead?>(new StoredFileRead(metadata, new MemoryStream(bytes, writable: false)));
        }
    }
}

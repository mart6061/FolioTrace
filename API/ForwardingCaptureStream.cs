namespace API;

internal sealed class ForwardingCaptureStream(Stream destination, int maximumCapturedBytes) : Stream
{
    private readonly MemoryStream captured = new(Math.Max(0, maximumCapturedBytes));
    private readonly int maximumCapturedBytes = Math.Max(0, maximumCapturedBytes);

    public ReadOnlyMemory<byte> CapturedBytes => captured.ToArray();

    public bool IsTruncated => TotalBytes > captured.Length;

    public long TotalBytes { get; private set; }

    public override bool CanRead => false;
    public override bool CanSeek => false;
    public override bool CanWrite => destination.CanWrite;
    public override long Length => TotalBytes;
    public override long Position { get => TotalBytes; set => throw new NotSupportedException(); }

    public override void Flush() => destination.Flush();

    public override Task FlushAsync(CancellationToken cancellationToken) => destination.FlushAsync(cancellationToken);

    public override void Write(byte[] buffer, int offset, int count)
    {
        destination.Write(buffer, offset, count);
        Capture(buffer.AsSpan(offset, count));
    }

    public override void Write(ReadOnlySpan<byte> buffer)
    {
        destination.Write(buffer);
        Capture(buffer);
    }

    public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        await destination.WriteAsync(buffer.AsMemory(offset, count), cancellationToken);
        Capture(buffer.AsSpan(offset, count));
    }

    public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        await destination.WriteAsync(buffer, cancellationToken);
        Capture(buffer.Span);
    }

    public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            captured.Dispose();
        base.Dispose(disposing);
    }

    private void Capture(ReadOnlySpan<byte> buffer)
    {
        TotalBytes += buffer.Length;
        var remaining = maximumCapturedBytes - (int)captured.Length;
        if (remaining > 0)
            captured.Write(buffer[..Math.Min(remaining, buffer.Length)]);
    }
}

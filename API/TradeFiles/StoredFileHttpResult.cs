using Repository;

namespace API.TradeFiles;

public sealed class StoredFileHttpResult(Guid storedFileID, IStoredFileRepository repository) : IResult
{
    public async Task ExecuteAsync(HttpContext context)
    {
        var cancellationToken = context.RequestAborted;
        var metadata = await repository.GetMetadataAsync(storedFileID, cancellationToken);
        if (metadata is null)
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        context.Response.Headers.AcceptRanges = "bytes";
        context.Response.ContentType = metadata.MediaType;
        var safeFileName = metadata.FileName.Replace("\"", string.Empty, StringComparison.Ordinal);
        context.Response.Headers.ContentDisposition = $"attachment; filename=\"{safeFileName}\"; filename*=UTF-8''{Uri.EscapeDataString(metadata.FileName)}";

        var rangeHeader = context.Request.Headers.Range.ToString();
        var range = string.IsNullOrWhiteSpace(rangeHeader)
            ? null
            : ParseRange(rangeHeader, metadata.ContentLength);
        if (!string.IsNullOrWhiteSpace(rangeHeader) && range is null)
        {
            context.Response.StatusCode = StatusCodes.Status416RangeNotSatisfiable;
            context.Response.Headers.ContentRange = $"bytes */{metadata.ContentLength}";
            return;
        }

        var offset = range?.Start ?? 0;
        var length = range?.Length ?? metadata.ContentLength;
        await using var file = await repository.OpenReadAsync(storedFileID, offset, range?.Length, cancellationToken);
        if (file is null)
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        context.Response.ContentLength = length;
        if (range is not null)
        {
            context.Response.StatusCode = StatusCodes.Status206PartialContent;
            context.Response.Headers.ContentRange = $"bytes {range.Value.Start}-{range.Value.End}/{metadata.ContentLength}";
        }

        await file.Content.CopyToAsync(context.Response.Body, cancellationToken);
    }

    internal static ByteRange? ParseRange(string value, long contentLength)
    {
        if (contentLength <= 0 || !value.StartsWith("bytes=", StringComparison.OrdinalIgnoreCase))
            return null;

        var specification = value[6..].Trim();
        if (specification.Contains(','))
            return null;

        var parts = specification.Split('-', 2);
        if (parts.Length != 2)
            return null;

        if (parts[0].Length == 0)
        {
            if (!long.TryParse(parts[1], out var suffixLength) || suffixLength <= 0)
                return null;
            suffixLength = Math.Min(suffixLength, contentLength);
            return new ByteRange(contentLength - suffixLength, contentLength - 1);
        }

        if (!long.TryParse(parts[0], out var start) || start < 0 || start >= contentLength)
            return null;

        var end = contentLength - 1;
        if (parts[1].Length > 0 && (!long.TryParse(parts[1], out end) || end < start))
            return null;

        end = Math.Min(end, contentLength - 1);
        return new ByteRange(start, end);
    }

    internal readonly record struct ByteRange(long Start, long End)
    {
        public long Length => End - Start + 1;
    }
}

namespace Repository;

public sealed record ApiExchange
{
    public Guid Id { get; init; } = Guid.CreateGuid7();

    public DateTime StartedAtUtc { get; init; }

    public DateTime CompletedAtUtc { get; init; }

    public long DurationMilliseconds { get; init; }

    public string Method { get; init; } = string.Empty;

    public string Path { get; init; } = string.Empty;

    public string QueryString { get; init; } = string.Empty;

    public int? StatusCode { get; init; }

    public string? ExceptionType { get; init; }

    public string? ExceptionMessage { get; init; }

    public ApiHttpMessage Request { get; init; } = new();

    public ApiHttpMessage Response { get; init; } = new();
}

public sealed record ApiHttpMessage
{
    public Dictionary<string, string[]> Headers { get; init; } = [];

    public string? Body { get; init; }

    public string? ContentType { get; init; }

    public long? ContentLength { get; init; }

    public bool BodyTruncated { get; init; }
}

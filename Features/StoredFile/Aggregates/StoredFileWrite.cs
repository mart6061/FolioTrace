namespace FolioTrace.Aggregates;

public sealed record StoredFileWrite(
    Guid Id,
    string FileName,
    string MediaType,
    long ContentLength,
    string SHA256,
    Stream Content);

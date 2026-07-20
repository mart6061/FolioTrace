namespace FolioTrace.Aggregates;

public sealed record StoredFileMetadata(
    Guid Id,
    string FileName,
    string MediaType,
    long ContentLength,
    string SHA256);

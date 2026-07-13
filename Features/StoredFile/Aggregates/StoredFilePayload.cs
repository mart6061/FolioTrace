namespace FolioTrace.Aggregates;
public sealed record StoredFilePayload(Guid Id, string FileName, string MediaType, string SHA256, byte[] Content);

using FolioTrace.Aggregates;

namespace Repository;

public interface IStoredFileRepository
{
    Task InitializeAsync(CancellationToken cancellationToken = default);

    Task<StoredFileMetadata?> GetMetadataAsync(Guid id, CancellationToken cancellationToken = default);

    Task<StoredFileRead?> OpenReadAsync(
        Guid id,
        long offset = 0,
        long? length = null,
        CancellationToken cancellationToken = default);
}

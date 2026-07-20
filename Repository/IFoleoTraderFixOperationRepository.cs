using FolioTrace.Aggregates;

namespace Repository;

public interface IFoleoTraderFixOperationRepository
{
    Task<FoleoTraderFixOperationSearchResult> SearchAsync(
        FoleoTraderFixOperationSearchCriteria criteria,
        CancellationToken cancellationToken = default);
}

public sealed record FoleoTraderFixOperationSearchCriteria(
    DateTime? FromUtc,
    DateTime? ToUtc,
    string? Direction,
    string? Channel,
    string? MsgType,
    string? ClOrdID,
    string? ExecID,
    string? Text,
    int Page,
    int PageSize);

public sealed record FoleoTraderFixOperationSearchResult(
    IReadOnlyList<FoleoTraderFIXOperationRecordedEvent> Items,
    int TotalCount,
    int Page,
    int PageSize);

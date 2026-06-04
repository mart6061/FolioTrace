using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record FXDefinitionReadModel
{
    public required string Id { get; init; }

    public required string Pair { get; init; }

    public required string BaseCurrency { get; init; }

    public required string QuoteCurrency { get; init; }

    public required string DisplayPair { get; init; }

    public required Active Active { get; init; }

    public required DateTime ValidFrom { get; init; }

    public required DateTime ValidTo { get; init; }

    public required Guid LastEventID { get; init; }

    public required DateTime LastAuditDateTime { get; init; }
}

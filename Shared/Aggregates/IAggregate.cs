using AILibrary.Common;
using AILibrary.Types;

namespace AILibrary.Aggregates;

// Marker interface for domain models
public interface IAggregate : IType
{
    LastAuditDateTime LastAuditDateTime { get; }

    EventDateTime ValuationDateTime { get; }

    AuditDateTime AsOfDateTime { get; }
}

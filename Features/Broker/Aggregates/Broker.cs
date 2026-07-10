using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record Broker : IModel
{
    public required string Name { get; init; }

    public required LegalEntityIdentifier LEI { get; init; }

    public required FeeRate Commission { get; init; }

    public required Active Active { get; init; }

    public required EventDateTime ApprovedDateTime { get; init; }

    public required EventDateTime NextReview { get; init; }

    public required string Notes { get; init; }

    public required List<ITradeMethod> TradeMethods { get; init; }

    public required EventDateTime ValuationDateTime { get; init; }

    public required AuditDateTime AsOfDateTime { get; init; }

    public required EventID LastEventID { get; init; }

    public required LastAuditDateTime LastAuditDateTime { get; init; }

    [JsonConstructor]
    [SetsRequiredMembers]
    public Broker(string name, LegalEntityIdentifier lei, FeeRate commission, Active active, EventDateTime approvedDateTime, EventDateTime nextReview, string notes, List<ITradeMethod> tradeMethods, EventDateTime valuationDateTime, AuditDateTime asOfDateTime, EventID lastEventID, LastAuditDateTime lastAuditDateTime)
    {
        Name = name;
        LEI = lei;
        Commission = commission;
        Active = active;
        ApprovedDateTime = approvedDateTime;
        NextReview = nextReview;
        Notes = notes;
        TradeMethods = tradeMethods;
        ValuationDateTime = valuationDateTime;
        AsOfDateTime = asOfDateTime;
        LastEventID = lastEventID;
        LastAuditDateTime = lastAuditDateTime;
    }

    [SetsRequiredMembers]
    public Broker(string name, LegalEntityIdentifier lei, FeeRate commission, Active active, EventDateTime approvedDateTime, EventDateTime nextReview, string notes, List<ITradeMethod> tradeMethods, EventDateTime valuationDateTime, AuditDateTime auditDateTime, EventID lastEventID)
        : this(name, lei, commission, active, approvedDateTime, nextReview, notes, tradeMethods, valuationDateTime, auditDateTime, lastEventID, new LastAuditDateTime(auditDateTime.Value))
    {
    }

    public override string ToString() => Name;
}

using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record HoldingNominalFeesAdministrator : HoldingFeesBase
{
    [JsonConstructor]
    [SetsRequiredMembers]
    public HoldingNominalFeesAdministrator(HoldingID holdingID, AccountID accountID, InstrumentID instrumentID, string name, Active active, bool isDefault, EventDateTime valuationDateTime, AuditDateTime asOfDateTime, EventID lastEventID, LastAuditDateTime lastAuditDateTime)
        : base(holdingID, accountID, instrumentID, name, active, isDefault, valuationDateTime, asOfDateTime, lastEventID, lastAuditDateTime)
    {
    }
}

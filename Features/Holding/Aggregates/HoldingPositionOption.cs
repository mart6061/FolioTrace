using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record HoldingPositionOption : HoldingBase, IHoldingPosition
{
    [JsonConstructor]
    [SetsRequiredMembers]
    public HoldingPositionOption(HoldingID holdingID, AccountID accountID, InstrumentID instrumentID, string name, Active active, bool @default, EventDateTime valuationDateTime, AuditDateTime asOfDateTime, EventID lastEventID, LastAuditDateTime lastAuditDateTime)
        : base(holdingID, accountID, instrumentID, name, active, @default, valuationDateTime, asOfDateTime, lastEventID, lastAuditDateTime)
    {
    }
}

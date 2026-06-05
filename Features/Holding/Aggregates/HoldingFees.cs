using System.Diagnostics.CodeAnalysis;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public abstract record HoldingFees : Holding, IHoldingNominal
{
    [SetsRequiredMembers]
    protected HoldingFees(HoldingID holdingID, AccountID accountID, InstrumentID instrumentID, string name, Active active, bool isDefault, EventDateTime valuationDateTime, AuditDateTime asOfDateTime, EventID lastEventID, LastAuditDateTime lastAuditDateTime)
        : base(holdingID, accountID, instrumentID, name, active, isDefault, valuationDateTime, asOfDateTime, lastEventID, lastAuditDateTime)
    {
    }
}

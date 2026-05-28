using System.ComponentModel;

namespace FolioTrace.Aggregates;

public enum ValuationDateBasis
{
    [Description("Execution")]
    EventDateTime,

    [Description("Settlement")]
    SettlementDateTime
}

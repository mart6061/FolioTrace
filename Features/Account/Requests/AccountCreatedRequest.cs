using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record AccountCreatedRequest(
    UserID UserID,
    EventDateTime EventDateTime,
    string Reason,
    AccountID? AccountID,
    string Name,
    string FormalName,
    Alpha3 BookCurrency,
    bool Active,
    ProfitLossMethod BookCostBasis = ProfitLossMethod.FIFO) : IEventRequest;

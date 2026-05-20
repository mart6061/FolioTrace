using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record CurrencyCreatedRequest(
    UserID UserID,
    EventDateTime EventDateTime,
    string Reason,
    Alpha3 AlphabeticCode,
    int NumericCode,
    short DecimalPlace,
    string Name) : IEventRequest;

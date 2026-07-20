namespace FolioTrace.Aggregates;

public sealed record FoleoTraderOrderValidationResult(
    IReadOnlyList<string> ValidationErrors,
    Ticket? Ticket,
    Instrument? Instrument,
    string SecurityID,
    string SecurityIDSource,
    string Symbol);

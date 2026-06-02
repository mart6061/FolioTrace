namespace FolioTrace.Aggregates;

public sealed record TicketFill(Guid FillID, decimal Price, decimal Quantity, string Note);

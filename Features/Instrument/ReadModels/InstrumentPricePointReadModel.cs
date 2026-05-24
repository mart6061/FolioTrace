namespace FolioTrace.Aggregates;

public sealed class InstrumentPricePointReadModel
{
    public string Id { get; set; } = string.Empty;
    public Guid InstrumentID { get; set; }
    public string PriceType { get; set; } = string.Empty;
    public decimal? Bid { get; set; }
    public decimal? Mid { get; set; }
    public decimal? Ask { get; set; }
    public decimal? Nav { get; set; }
    public string Currency { get; set; } = string.Empty;
    public DateTime ValidFrom { get; set; }
    public DateTime ValidTo { get; set; }
    public Guid LastEventID { get; set; }
    public DateTime LastAuditDateTime { get; set; }
}

namespace FolioTrace.Aggregates;

public sealed class InstrumentIncomePointReadModel
{
    public string Id { get; set; } = string.Empty;
    public Guid InstrumentID { get; set; }
    public string IncomeType { get; set; } = string.Empty;
    public decimal? Income { get; set; }
    public string Currency { get; set; } = string.Empty;
    public DateTime ValidFrom { get; set; }
    public DateTime ValidTo { get; set; }
    public Guid LastEventID { get; set; }
    public DateTime LastAuditDateTime { get; set; }
}

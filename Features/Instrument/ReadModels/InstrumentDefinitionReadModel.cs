namespace FolioTrace.Aggregates;

public sealed class InstrumentDefinitionReadModel
{
    public string Id { get; set; } = string.Empty;
    public Guid InstrumentID { get; set; }
    public string Name { get; set; } = string.Empty;
    public string FormalName { get; set; } = string.Empty;
    public string Exchange { get; set; } = string.Empty;
    public string CFI { get; set; } = string.Empty;
    public bool Active { get; set; }
    public string IncomeCountry { get; set; } = string.Empty;
    public string PriceCountry { get; set; } = string.Empty;
    public DateTime ValidFrom { get; set; }
    public DateTime ValidTo { get; set; }
    public Guid LastEventID { get; set; }
    public DateTime LastAuditDateTime { get; set; }
}

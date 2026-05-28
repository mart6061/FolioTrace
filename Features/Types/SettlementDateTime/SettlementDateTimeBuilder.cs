namespace FolioTrace.Types;

public static class SettlementDateTimeBuilder
{
    public static SettlementDateTime Create() => new(DateTime.UtcNow);

    public static SettlementDateTime Create(DateTime value) => new(value);
}

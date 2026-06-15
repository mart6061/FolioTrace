using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public abstract record FXPriceValue : Price
{
    public decimal Value => Amount;

    protected FXPriceValue(decimal value)
        : base(value, allowZero: true)
    {
    }

    protected FXPriceValue()
    {
    }

    public override string ToString() => Value.ToString("0.########");
}

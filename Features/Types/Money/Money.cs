using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace FolioTrace.Types;

public sealed record Money : IType
{
    public required decimal Amount { get; init; }

    public required Alpha3 Currency { get; init; }

    [JsonConstructor]
    [SetsRequiredMembers]
    public Money(decimal amount, Alpha3 currency)
    {
        if (currency is null)
            throw new ArgumentNullException(nameof(currency));

        if (decimal.Round(amount, 8) != amount)
            throw new ArgumentException("Money can have at most 8 decimal places.", nameof(amount));

        Amount = amount;
        Currency = currency;
    }

    public static Money operator +(Money left, Money right)
    {
        EnsureSameCurrency(left, right);
        return new Money(left.Amount + right.Amount, left.Currency);
    }

    public static Money operator -(Money left, Money right)
    {
        EnsureSameCurrency(left, right);
        return new Money(left.Amount - right.Amount, left.Currency);
    }

    public override string ToString() => $"{Amount:0.########} {Currency.Value}";

    private static void EnsureSameCurrency(Money left, Money right)
    {
        if (left is null)
            throw new ArgumentNullException(nameof(left));

        if (right is null)
            throw new ArgumentNullException(nameof(right));

        if (left.Currency != right.Currency)
            throw new InvalidOperationException($"Money currencies must match. Left '{left.Currency}', right '{right.Currency}'.");
    }
}

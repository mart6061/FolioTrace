using System.Text.Json.Serialization;

namespace FolioTrace.Types;

[JsonConverter(typeof(ContractMultiplierJsonConverter))]
public sealed record ContractMultiplier : IType
{
    public decimal Value { get; init; }

    public ContractMultiplier(decimal value)
    {
        if (value <= 0m)
            throw new ArgumentException("Contract multiplier must be greater than zero.", nameof(value));

        if (decimal.Round(value, 8) != value)
            throw new ArgumentException("Contract multiplier can have at most 8 decimal places.", nameof(value));

        Value = value;
    }

    [JsonConstructor]
    private ContractMultiplier() { }

    internal static ContractMultiplier FromJson(decimal value) => new(value);

    public static implicit operator decimal(ContractMultiplier value) => value?.Value ?? 1m;

    public override string ToString() => Value.ToString("0.########");
}

using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace FolioTrace.Aggregates;

public sealed record CountryFlag
{
    public required string Svg { get; init; }

    [JsonConstructor]
    [SetsRequiredMembers]
    public CountryFlag(string svg)
    {
        if (string.IsNullOrWhiteSpace(svg))
            throw new ArgumentException("Value must not be null, empty, or whitespace.", nameof(svg));

        Svg = svg;
    }

    public override string ToString() => Svg;

    public string ToData() => Svg;

    public string ToDetail() => $"{nameof(CountryFlag)}: (Svg: {Svg})";
}

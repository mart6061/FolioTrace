namespace FolioTrace.Aggregates;

public sealed record InstrumentLogo
{
    public string Svg { get; init; } = string.Empty;

    public InstrumentLogo(string svg)
    {
        if (string.IsNullOrWhiteSpace(svg))
            throw new ArgumentException("Logo SVG is required.", nameof(svg));

        if (!svg.TrimStart().StartsWith("<svg", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Logo must be an SVG document.", nameof(svg));

        Svg = svg;
    }

    private InstrumentLogo()
    {
    }

    public override string ToString() => Svg;
}

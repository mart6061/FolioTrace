namespace FolioTrace.Types;

public sealed record CFICategory(char Code, string Name) : IType
{
    public static CFICategory From(char code) =>
        code switch
        {
            'C' => new CFICategory(code, "Collective investment vehicles"),
            'D' => new CFICategory(code, "Debt instruments"),
            'E' => new CFICategory(code, "Equities"),
            'F' => new CFICategory(code, "Futures"),
            'M' => new CFICategory(code, "Others/Miscellaneous"),
            'O' => new CFICategory(code, "Options"),
            'R' => new CFICategory(code, "Entitlements"),
            'S' => new CFICategory(code, "Swaps"),
            _ => new CFICategory(code, "Unknown")
        };

    public bool IsKnown => Name != "Unknown";

    public override string ToString() => Name;
}

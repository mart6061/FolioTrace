namespace FolioTrace.Types;

public sealed record CFIGroup(char CategoryCode, char Code, string Name) : IType
{
    public static CFIGroup From(char categoryCode, char groupCode) =>
        new(categoryCode, groupCode, GetName(categoryCode, groupCode));

    public bool IsKnown => Name != "Unknown";

    public string ToData() => Code.ToString();

    public string ToDetail() => $"{nameof(CFIGroup)}: {Name} ({CategoryCode}{Code})";

    public override string ToString() => Name;

    private static string GetName(char categoryCode, char groupCode) =>
        (categoryCode, groupCode) switch
        {
            ('C', 'I') => "Standard investment funds/mutual funds",
            ('C', 'E') => "Exchange-traded funds",
            ('C', 'B') => "Real estate investment trusts",

            ('D', 'B') => "Bonds",
            ('D', 'C') => "Convertible bonds",
            ('D', 'S') => "Structured instruments",
            ('D', 'T') => "Medium-term notes",
            ('D', 'W') => "Bonds with warrants attached",
            ('D', 'Y') => "Money market instruments",

            ('E', 'S') => "Shares",
            ('E', 'P') => "Preferred shares",
            ('E', 'C') => "Convertible shares",
            ('E', 'F') => "Preferred convertible shares",
            ('E', 'U') => "Units",
            ('E', 'M') => "Others",

            ('F', 'F') => "Financial futures",
            ('F', 'C') => "Commodities futures",

            ('O', 'C') => "Call options",
            ('O', 'P') => "Put options",
            ('O', 'M') => "Others",

            ('R', 'A') => "Allotment rights",
            ('R', 'S') => "Subscription rights",
            ('R', 'P') => "Purchase rights",
            ('R', 'W') => "Warrants",

            ('S', 'W') => "Swaps",

            _ => "Unknown"
        };
}

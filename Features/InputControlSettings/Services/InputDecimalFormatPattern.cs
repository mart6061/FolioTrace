namespace FolioTrace.Aggregates;

public static class InputDecimalFormatPattern
{
    public static bool IsValid(string? pattern)
    {
        if (string.IsNullOrWhiteSpace(pattern))
            return false;

        var trimmed = pattern.Trim();
        if (trimmed.Any(character => character is not ('#' or '0' or ',' or '.')))
            return false;

        if (trimmed.Count(character => character == '.') > 1)
            return false;

        var parts = trimmed.Split('.');
        var integerPart = parts[0];
        var decimalPart = parts.Length == 2 ? parts[1] : string.Empty;

        if (!integerPart.Any(character => character is '#' or '0'))
            return false;

        if (integerPart.EndsWith(',') || integerPart.StartsWith(','))
            return false;

        if (integerPart.Contains(",,", StringComparison.Ordinal))
            return false;

        if (decimalPart.Contains(','))
            return false;

        return decimalPart.All(character => character is '#' or '0');
    }

    public static int FixedDecimalPlaces(string? pattern)
    {
        var decimalPart = DecimalPart(pattern);
        return decimalPart.Count(character => character == '0');
    }

    public static int OptionalDecimalPlaces(string? pattern)
    {
        var decimalPart = DecimalPart(pattern);
        return decimalPart.Count(character => character == '#');
    }

    public static bool UsesGrouping(string? pattern)
    {
        var integerPart = (pattern ?? string.Empty).Split('.')[0];
        return integerPart.Contains(',', StringComparison.Ordinal);
    }

    private static string DecimalPart(string? pattern)
    {
        if (string.IsNullOrWhiteSpace(pattern))
            return string.Empty;

        var parts = pattern.Trim().Split('.');
        return parts.Length == 2 ? parts[1] : string.Empty;
    }
}

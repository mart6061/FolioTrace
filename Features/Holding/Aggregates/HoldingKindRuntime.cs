namespace FolioTrace.Aggregates;

public static class HoldingKindRuntime
{
    extension(Holding holding)
    {
        public string GetHoldingKindName() =>
        GetKindName(holding?.GetType() ?? throw new ArgumentNullException(nameof(holding)));
    }

    extension(HoldingCreatedEvent holdingEvent)
    {
        public string GetHoldingKindName() =>
        GetEventKindName(holdingEvent?.GetType() ?? throw new ArgumentNullException(nameof(holdingEvent)), "CreatedEvent");
    }

    extension(HoldingModifiedEvent holdingEvent)
    {
        public string GetHoldingKindName() =>
        GetEventKindName(holdingEvent?.GetType() ?? throw new ArgumentNullException(nameof(holdingEvent)), "ModifiedEvent");
    }


    public static string GetKindName<T>() => GetKindName(typeof(T));

    public static string GetKindName(Type type)
    {
        if (type is null)
            throw new ArgumentNullException(nameof(type));

        return type.Name.StartsWith("Holding", StringComparison.Ordinal)
            ? type.Name["Holding".Length..]
            : type.Name;
    }

    public static bool IsPositionCash<T>() => typeof(T) == typeof(HoldingPositionCash);

    public static bool IsPositionMemo<T>() => typeof(T) == typeof(HoldingPositionMemo);

    public static bool IsPositionAsset<T>() => typeof(T) == typeof(HoldingPositionAsset);

    private static string GetEventKindName(Type type, string suffix)
    {
        var name = GetKindName(type);
        return name.EndsWith(suffix, StringComparison.Ordinal)
            ? name[..^suffix.Length]
            : name;
    }
}

using System.Text.Json;

namespace Services;

public static class CacheMemoryEstimator
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static long EstimateBytes<T>(IEnumerable<T> values)
    {
        long total = 0;

        foreach (var value in values)
            total += EstimateBytes(value);

        return total;
    }

    private static long EstimateBytes<T>(T value)
    {
        if (value is null)
            return 0;

        return JsonSerializer.SerializeToUtf8Bytes(value, value.GetType(), JsonOptions).LongLength;
    }
}

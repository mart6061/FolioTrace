using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using FolioTrace.Common;

namespace API;

public static partial class EventPropertyDetailsFactory
{
    private static readonly JsonNamingPolicy JsonNamingPolicy = JsonNamingPolicy.CamelCase;
    private static readonly ConcurrentDictionary<Type, IReadOnlyList<ResponseProperty>> ResponsePropertiesByType = [];
    private static readonly ConcurrentDictionary<Type, IReadOnlyDictionary<string, EventPropertyMetadata>> PropertyMetadataByEventType = [];
    private static readonly HashSet<string> ExcludedProperties = new(StringComparer.OrdinalIgnoreCase)
    {
        "$type",
        "applicationStatus",
        "details",
        "propertyDetails"
    };

    public static EventResponseWithPropertyDetails WithPropertyDetails(IEventBase @event, object response, object? nestedDetails = null)
    {
        var properties = ToExtensionProperties(response);
        var propertyDetails = CreatePropertyDetails(@event, response)
            .Concat(nestedDetails is null ? [] : CreatePropertyDetails(@event, nestedDetails))
            .Select((detail, index) => new { Detail = detail, Index = index })
            .OrderBy(item => item.Detail.Order)
            .ThenBy(item => item.Index)
            .Select(item => item.Detail)
            .ToList();

        return new EventResponseWithPropertyDetails(properties, propertyDetails);
    }

    private static Dictionary<string, object?> ToExtensionProperties(object response)
    {
        var properties = new Dictionary<string, object?>(StringComparer.Ordinal);

        foreach (var property in GetResponseProperties(response.GetType()))
            properties[property.JsonName] = property.GetValue(response);

        return properties;
    }

    private static IReadOnlyList<EventPropertyDetail> CreatePropertyDetails(IEventBase @event, object response)
    {
        var metadataByProperty = GetPropertyMetadata(@event.GetType());
        var details = new List<EventPropertyDetail>();

        foreach (var property in GetResponseProperties(response.GetType()))
        {
            if (ExcludedProperties.Contains(property.JsonName) || ExcludedProperties.Contains(property.Name))
                continue;

            metadataByProperty.TryGetValue(property.Name, out var metadata);

            var description = string.IsNullOrWhiteSpace(metadata?.Description)
                ? FormatPropertyName(property.Name)
                : metadata.Description;

            details.Add(new EventPropertyDetail(property.Name, description, metadata?.Order ?? int.MaxValue, property.GetValue(response)));
        }

        return details;
    }

    private static IReadOnlyList<ResponseProperty> GetResponseProperties(Type responseType) =>
        ResponsePropertiesByType.GetOrAdd(responseType, static type =>
            type.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(property => property.GetMethod is not null && property.GetIndexParameters().Length == 0)
                .Select(property => new ResponseProperty(
                    property.Name,
                    JsonNamingPolicy.ConvertName(property.Name),
                    property.GetValue))
                .ToList());

    private static IReadOnlyDictionary<string, EventPropertyMetadata> GetPropertyMetadata(Type eventType) =>
        PropertyMetadataByEventType.GetOrAdd(eventType, static type =>
            GetMetadataTypes(type)
                .SelectMany(metadataType => metadataType.GetProperties(BindingFlags.Instance | BindingFlags.Public))
                .Where(property => property.GetIndexParameters().Length == 0)
                .Select(property => new
                {
                    property.Name,
                    Metadata = property.GetCustomAttribute<EventPropertyAttribute>()
                })
                .Where(property => property.Metadata is not null)
                .ToDictionary(
                    property => property.Name,
                    property => new EventPropertyMetadata(property.Metadata!.Description, property.Metadata.Order),
                    StringComparer.OrdinalIgnoreCase));

    private static IEnumerable<Type> GetMetadataTypes(Type eventType)
    {
        yield return typeof(IEventBase);

        for (var type = eventType; type is not null; type = type.BaseType)
            yield return type;

        foreach (var interfaceType in eventType.GetInterfaces())
        {
            if (interfaceType != typeof(IEventBase))
                yield return interfaceType;
        }
    }

    private static string FormatPropertyName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return name;

        var beforeAcronymBoundary = AcronymBoundaryRegex().Replace(name, "$1 $2");
        return WordBoundaryRegex().Replace(beforeAcronymBoundary, "$1 $2");
    }

    private sealed record ResponseProperty(string Name, string JsonName, Func<object, object?> GetValue);
    private sealed record EventPropertyMetadata(string? Description, int Order);

    [GeneratedRegex("([A-Z]+)([A-Z][a-z])")]
    private static partial Regex AcronymBoundaryRegex();

    [GeneratedRegex("([a-z0-9])([A-Z])")]
    private static partial Regex WordBoundaryRegex();
}

public sealed record EventPropertyDetail(string Name, string Description, int Order, object? Value);

public sealed class EventResponseWithPropertyDetails
{
    public EventResponseWithPropertyDetails()
        : this(new Dictionary<string, object?>(), [])
    {
    }

    public EventResponseWithPropertyDetails(IDictionary<string, object?> properties, IReadOnlyList<EventPropertyDetail> propertyDetails)
    {
        Properties = properties;
        PropertyDetails = propertyDetails;
    }

    [JsonExtensionData]
    public IDictionary<string, object?> Properties { get; }

    public IReadOnlyList<EventPropertyDetail> PropertyDetails { get; }
}

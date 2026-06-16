using System.Text.RegularExpressions;
using System.Reflection;
using System.Text.Json.Serialization;
using FolioTrace.Aggregates;
using FolioTrace.Common;

namespace Test;

public sealed class EventClassAttributeTests
{
    private static readonly HashSet<string> AuthenticationEvents =
    [
        nameof(UserSignedInEvent),
        nameof(UserSignedOutEvent)
    ];

    private static readonly HashSet<string> SystemEvents =
    [
        nameof(FoleoTraderOrderSubmittedEvent),
        nameof(FoleoTraderExecutionReceivedEvent),
        nameof(FoleoTraderOrderFailedEvent),
        nameof(FoleoTraderFIXOperationRecordedEvent)
    ];

    private static readonly HashSet<string> TransactionEvents =
    [
        nameof(TransactionCreditEvent),
        nameof(TransactionDebitEvent)
    ];

    private static readonly HashSet<string> ModifiedExceptionEvents =
    [
        nameof(TicketAccountAddedEvent),
        nameof(TicketAccountRemovedEvent),
        nameof(TicketProposalApprovedEvent),
        nameof(TicketProposalNotApprovedEvent),
        nameof(TicketProposalDecisionRequestedEvent),
        nameof(TicketTradeApprovedEvent),
        nameof(TicketTradeNotApprovedEvent),
        nameof(TicketTradeDecisionRequestedEvent),
        nameof(TicketTradeFillAddedEvent),
        nameof(TicketTradeFillRemovedEvent),
        nameof(UserBookmarkDeletedEvent)
    ];

    [Fact]
    public void AuditEventBaseDescendantsHaveEventClassAttribute()
    {
        var missing = IncludedEventTypes()
            .Where(type => type.GetCustomAttributes(typeof(EventClassAttribute), inherit: false).Length == 0)
            .Select(type => type.Name)
            .Order()
            .ToList();

        Assert.Empty(missing);
    }

    [Fact]
    public void EventClassAttributeEventTypesMatchNamingRules()
    {
        var mismatches = IncludedEventTypes()
            .Select(type => new
            {
                Type = type,
                Attribute = type.GetCustomAttributes(typeof(EventClassAttribute), inherit: false).Cast<EventClassAttribute>().Single()
            })
            .Where(item => item.Attribute.EventType != ExpectedEventType(item.Type.Name))
            .Select(item => $"{item.Type.Name}: expected {ExpectedEventType(item.Type.Name)}, actual {item.Attribute.EventType}")
            .Order()
            .ToList();

        Assert.Empty(mismatches);
    }

    [Fact]
    public void EventClassAttributeDescriptionsMatchClassNames()
    {
        var mismatches = IncludedEventTypes()
            .Select(type => new
            {
                Type = type,
                Attribute = type.GetCustomAttributes(typeof(EventClassAttribute), inherit: false).Cast<EventClassAttribute>().Single()
            })
            .Where(item => item.Attribute.Description != FormatEventDescription(item.Type.Name))
            .Select(item => $"{item.Type.Name}: expected {FormatEventDescription(item.Type.Name)}, actual {item.Attribute.Description}")
            .Order()
            .ToList();

        Assert.Empty(mismatches);
    }

    [Fact]
    public void EventClassAttributeValidationOmitsEventBaseTypes()
    {
        var includedNames = IncludedEventTypes().Select(type => type.Name).ToHashSet();

        Assert.DoesNotContain(nameof(AuditEventBase), includedNames);
        Assert.DoesNotContain(nameof(ConfigEventBase), includedNames);
        Assert.DoesNotContain(nameof(EventBase), includedNames);
        Assert.DoesNotContain(nameof(TicketEventBase), includedNames);
        Assert.DoesNotContain(nameof(TicketProposalEventBase), includedNames);
        Assert.DoesNotContain(nameof(TicketTradeEventBase), includedNames);
        Assert.DoesNotContain(nameof(TicketTradeFillEventBase), includedNames);
    }

    [Fact]
    public void EventBaseAndConfigEventBaseFollowAuditBaseConventions()
    {
        Assert.True(typeof(IAuditEventBase).IsAssignableFrom(typeof(IEventBase)));
        Assert.True(typeof(AuditEventBase).IsAssignableFrom(typeof(EventBase)));
        Assert.True(typeof(IAuditEventBase).IsAssignableFrom(typeof(IConfigEventBase)));
        Assert.True(typeof(AuditEventBase).IsAssignableFrom(typeof(ConfigEventBase)));

        Assert.Contains(nameof(IEventBase.EventDateTime), InterfacePropertyNames(typeof(IEventBase)));
        Assert.Contains(nameof(IEventBase.Reason), InterfacePropertyNames(typeof(IEventBase)));
        Assert.DoesNotContain(nameof(IEventBase.EventDateTime), InterfacePropertyNames(typeof(IConfigEventBase)));
        Assert.DoesNotContain(nameof(IEventBase.Reason), InterfacePropertyNames(typeof(IConfigEventBase)));
        Assert.DoesNotContain(nameof(IEventBase.EventDateTime), PublicPropertyNames(typeof(ConfigEventBase)));
        Assert.DoesNotContain(nameof(IEventBase.Reason), PublicPropertyNames(typeof(ConfigEventBase)));

        Assert.All(
            IncludedEventTypes().Where(type => typeof(IConfigEventBase).IsAssignableFrom(type)),
            type => Assert.False(typeof(IEventBase).IsAssignableFrom(type), $"{type.Name} should not implement IEventBase."));
    }

    [Fact]
    public void EventPayloadPropertiesHaveEventPropertyAttribute()
    {
        var missing = IncludedEventTypes()
            .SelectMany(eventType =>
            {
                var metadata = EventPropertyMetadata(eventType)
                    .Select(item => item.Property.Name)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                return EventPayloadPropertyNames(eventType)
                    .Where(propertyName => !metadata.Contains(propertyName))
                    .Select(propertyName => $"{eventType.Name}.{propertyName}");
            })
            .Order()
            .ToList();

        Assert.Empty(missing);
    }

    [Fact]
    public void EventPropertyAttributeDescriptionsMatchPropertyNames()
    {
        var mismatches = IncludedEventTypes()
            .SelectMany(eventType => EventPropertyMetadata(eventType)
                .Select(item => new
                {
                    EventType = eventType,
                    item.Property,
                    item.Attribute
                }))
            .Where(item => item.Attribute.Description != FormatEventPropertyDescription(item.Property.Name))
            .Select(item => $"{item.EventType.Name}.{item.Property.Name}: expected {FormatEventPropertyDescription(item.Property.Name)}, actual {item.Attribute.Description}")
            .Order()
            .ToList();

        Assert.Empty(mismatches);
    }

    [Fact]
    public void EventPropertyAttributeMetadataHasOneSourcePerProperty()
    {
        var duplicates = IncludedEventTypes()
            .SelectMany(eventType => EventPropertyMetadata(eventType)
                .GroupBy(item => item.Property.Name, StringComparer.OrdinalIgnoreCase)
                .Where(group => group.Count() > 1)
                .Select(group => $"{eventType.Name}.{group.Key}: {string.Join(", ", group.Select(item => item.Property.DeclaringType!.Name).Order())}"))
            .Order()
            .ToList();

        Assert.Empty(duplicates);
    }

    private static IReadOnlyList<Type> IncludedEventTypes() =>
        typeof(AuditEventBase).Assembly
            .GetTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false })
            .Where(type => !type.IsNested)
            .Where(type => typeof(IAuditEventBase).IsAssignableFrom(type))
            .Where(type => !type.Name.EndsWith("Base", StringComparison.Ordinal))
            .OrderBy(type => type.Name)
            .ToList();

    private static IEnumerable<string> EventPayloadPropertyNames(Type eventType) =>
        GetEventMetadataTypes(eventType)
            .SelectMany(type => type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
            .Where(IsEventPayloadProperty)
            .Select(property => property.Name)
            .Distinct(StringComparer.OrdinalIgnoreCase);

    private static IEnumerable<(PropertyInfo Property, EventPropertyAttribute Attribute)> EventPropertyMetadata(Type eventType) =>
        GetEventMetadataTypes(eventType)
            .SelectMany(type => type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
            .Where(IsEventPayloadProperty)
            .Select(property => new
            {
                Property = property,
                Attribute = property.GetCustomAttribute<EventPropertyAttribute>(inherit: false)
            })
            .Where(item => item.Attribute is not null)
            .Select(item => (item.Property, item.Attribute!));

    private static IReadOnlySet<string> InterfacePropertyNames(Type type) =>
        type.GetProperties(BindingFlags.Instance | BindingFlags.Public).Select(property => property.Name).ToHashSet(StringComparer.Ordinal);

    private static IReadOnlySet<string> PublicPropertyNames(Type type) =>
        type.GetProperties(BindingFlags.Instance | BindingFlags.Public).Select(property => property.Name).ToHashSet(StringComparer.Ordinal);

    private static IEnumerable<Type> GetEventMetadataTypes(Type eventType)
    {
        for (var type = eventType; type is not null && type != typeof(EventBase); type = type.BaseType)
            yield return type;

        foreach (var interfaceType in eventType.GetInterfaces())
        {
            if (interfaceType != typeof(IEventBase))
                yield return interfaceType;
        }
    }

    private static bool IsEventPayloadProperty(PropertyInfo property) =>
        property.GetMethod is not null &&
        property.GetIndexParameters().Length == 0 &&
        property.GetCustomAttribute<JsonIgnoreAttribute>(inherit: false) is null &&
        property.DeclaringType != typeof(IAuditEventBase) &&
        property.DeclaringType != typeof(AuditEventBase) &&
        property.DeclaringType != typeof(IConfigEventBase) &&
        property.DeclaringType != typeof(ConfigEventBase) &&
        property.DeclaringType != typeof(IEventBase) &&
        property.DeclaringType != typeof(EventBase) &&
        property.Name != nameof(IEventBase.Type);

    private static EventClassTypeEnum ExpectedEventType(string name)
    {
        if (AuthenticationEvents.Contains(name))
            return EventClassTypeEnum.Authentication;

        if (SystemEvents.Contains(name))
            return EventClassTypeEnum.System;

        if (TransactionEvents.Contains(name))
            return EventClassTypeEnum.Transaction;

        if (ModifiedExceptionEvents.Contains(name))
            return EventClassTypeEnum.Modified;

        if (name.Contains("Cancel", StringComparison.Ordinal))
            return EventClassTypeEnum.Cancelled;

        if (name.Contains("Created", StringComparison.Ordinal))
            return EventClassTypeEnum.Created;

        if (name.Contains("Modified", StringComparison.Ordinal) ||
            name.Contains("Set", StringComparison.Ordinal) ||
            name.Contains("Unset", StringComparison.Ordinal))
            return EventClassTypeEnum.Modified;

        throw new InvalidOperationException($"No EventClassType mapping for {name}.");
    }

    private static string FormatEventDescription(string name)
    {
        var withWordBoundaries = Regex.Replace(name, "([a-z0-9])([A-Z])", "$1 $2");
        return PreserveKnownAcronyms(Regex.Replace(withWordBoundaries, "([A-Z]+)([A-Z][a-z])", "$1 $2"));
    }

    private static string FormatEventPropertyDescription(string name)
    {
        var beforeAcronymBoundary = Regex.Replace(name, "([A-Z]+)([A-Z][a-z])", "$1 $2");
        return PreserveKnownAcronyms(Regex.Replace(beforeAcronymBoundary, "([a-z0-9])([A-Z])", "$1 $2"));
    }

    private static string PreserveKnownAcronyms(string description) =>
        description
            .Replace("I Ds", "IDs", StringComparison.Ordinal)
            .Replace("I D", "ID", StringComparison.Ordinal)
            .Replace("F X", "FX", StringComparison.Ordinal)
            .Replace("C F I", "CFI", StringComparison.Ordinal)
            .Replace("I S I N", "ISIN", StringComparison.Ordinal)
            .Replace("B I C", "BIC", StringComparison.Ordinal)
            .Replace("I B A N", "IBAN", StringComparison.Ordinal)
            .Replace("L E I", "LEI", StringComparison.Ordinal);
}

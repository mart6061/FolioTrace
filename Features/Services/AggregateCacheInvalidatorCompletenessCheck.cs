using FolioTrace.Common;
using Microsoft.Extensions.DependencyInjection;

namespace Services;

public static class AggregateCacheInvalidatorCompletenessCheck
{
    public static void Validate(IServiceProvider serviceProvider)
    {
        var invalidators = serviceProvider.GetServices<IAggregateCacheInvalidator>()
            // The universal IAuditEventBase catch-all (e.g. AggregateUpdateNotificationService, which only
            // publishes UI notifications) trivially "covers" every event type via IsAssignableFrom and would
            // mask real per-aggregate gaps if treated as coverage, so it's excluded from both family
            // derivation and the coverage check below.
            .Where(invalidator => invalidator.EventType != typeof(IAuditEventBase))
            .ToList();
        var assembly = typeof(IAggregateCacheInvalidator).Assembly;

        var familyInterfaces = new HashSet<Type>();
        foreach (var invalidator in invalidators)
        {
            var eventType = invalidator.EventType;
            if (eventType.IsInterface)
            {
                familyInterfaces.Add(eventType);
                continue;
            }

            foreach (var leafInterface in LeafEventInterfaces(eventType))
                familyInterfaces.Add(leafInterface);
        }

        var missing = new List<string>();
        foreach (var family in familyInterfaces)
        {
            // Only publicly appendable types are reachable through IEventRepository.AppendAsync<T>; private
            // nested records (e.g. computation-internal marker/movement types reusing an event interface for
            // type compatibility) can never actually be appended and would otherwise be false positives.
            var concreteTypes = assembly.GetTypes()
                .Where(type => !type.IsAbstract && !type.IsInterface && (type.IsPublic || type.IsNestedPublic) && family.IsAssignableFrom(type));

            foreach (var concreteType in concreteTypes)
            {
                var covered = invalidators.Any(invalidator => invalidator.EventType.IsAssignableFrom(concreteType));
                if (!covered)
                    missing.Add($"{concreteType.Name} (implements {family.Name}, no matching IAggregateCacheInvalidator is registered)");
            }
        }

        if (missing.Count > 0)
            throw new InvalidOperationException(
                "Aggregate cache invalidator registration is incomplete. The following event types have no " +
                "registered IAggregateCacheInvalidator that covers them, so appending one would never invalidate " +
                "the affected aggregate's cache:" + Environment.NewLine + string.Join(Environment.NewLine, missing));
    }

    /// <summary>
    /// Returns the most-derived event-marker interfaces implemented by <paramref name="eventType"/> (excluding
    /// IAuditEventBase itself). Type.GetInterfaces() returns the full transitive closure, which would otherwise
    /// pull in broad shared ancestors like IEventBase - implemented by dozens of unrelated event families - as
    /// a "family" in its own right.
    /// </summary>
    private static IEnumerable<Type> LeafEventInterfaces(Type eventType)
    {
        var candidates = eventType.GetInterfaces()
            .Where(candidate => candidate != typeof(IAuditEventBase) && typeof(IAuditEventBase).IsAssignableFrom(candidate))
            .ToList();

        return candidates.Where(candidate => !candidates.Any(other => other != candidate && candidate.IsAssignableFrom(other)));
    }
}

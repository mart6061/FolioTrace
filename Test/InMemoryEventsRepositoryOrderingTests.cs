using System.Reflection;
using FolioTrace;
using FolioTrace.Aggregates;
using FolioTrace.Common;
using FolioTrace.Types;
using Repository;

namespace Test;

public sealed class InMemoryEventsRepositoryOrderingTests
{
    [Fact]
    public void FindInsertionIndex_AppendsForwardDatedEventsAtTheEnd()
    {
        var events = new List<IAuditEventBase>();

        Insert(events, CreatedAt(new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc), "11111111-1111-1111-1111-111111111111"));
        Insert(events, CreatedAt(new DateTime(2025, 2, 1, 0, 0, 0, DateTimeKind.Utc), "22222222-2222-2222-2222-222222222222"));
        Insert(events, CreatedAt(new DateTime(2025, 3, 1, 0, 0, 0, DateTimeKind.Utc), "33333333-3333-3333-3333-333333333333"));

        AssertOrdered(events);
        Assert.Equal(new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc), EventDateTimeOf(events[0]));
        Assert.Equal(new DateTime(2025, 2, 1, 0, 0, 0, DateTimeKind.Utc), EventDateTimeOf(events[1]));
        Assert.Equal(new DateTime(2025, 3, 1, 0, 0, 0, DateTimeKind.Utc), EventDateTimeOf(events[2]));
    }

    [Fact]
    public void FindInsertionIndex_InsertsABackdatedCorrectionInTheMiddle()
    {
        var events = new List<IAuditEventBase>();

        Insert(events, CreatedAt(new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc), "11111111-1111-1111-1111-111111111111"));
        Insert(events, CreatedAt(new DateTime(2025, 3, 1, 0, 0, 0, DateTimeKind.Utc), "22222222-2222-2222-2222-222222222222"));

        // A correction recorded now (late AuditDateTime) but with an EventDateTime that falls between the two
        // events already appended above.
        var backdated = CreatedAt(new DateTime(2025, 2, 1, 0, 0, 0, DateTimeKind.Utc), "33333333-3333-3333-3333-333333333333");
        Insert(events, backdated);

        AssertOrdered(events);
        Assert.Equal(3, events.Count);
        Assert.Same(backdated, events[1]);
    }

    [Fact]
    public void UpperBoundByEventDateTime_FindsTheCorrectBoundary()
    {
        var events = new List<IAuditEventBase>();
        Insert(events, CreatedAt(new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc), "11111111-1111-1111-1111-111111111111"));
        Insert(events, CreatedAt(new DateTime(2025, 2, 1, 0, 0, 0, DateTimeKind.Utc), "22222222-2222-2222-2222-222222222222"));
        Insert(events, CreatedAt(new DateTime(2025, 3, 1, 0, 0, 0, DateTimeKind.Utc), "33333333-3333-3333-3333-333333333333"));

        Assert.Equal(0, UpperBound(events, new DateTime(2024, 12, 31, 0, 0, 0, DateTimeKind.Utc)));
        Assert.Equal(1, UpperBound(events, new DateTime(2025, 1, 15, 0, 0, 0, DateTimeKind.Utc)));
        Assert.Equal(2, UpperBound(events, new DateTime(2025, 2, 1, 0, 0, 0, DateTimeKind.Utc)));
        Assert.Equal(3, UpperBound(events, new DateTime(2025, 12, 31, 0, 0, 0, DateTimeKind.Utc)));
    }

    [Fact]
    public void FindInsertionIndex_OfAnAlreadyPresentEvent_ReturnsTheIndexRightAfterIt()
    {
        // LoadStreamAfterAsync relies on this: FindInsertionIndex(events, boundary), where boundary is itself
        // already a member of events, must land exactly one past boundary's own position - not before it, not
        // further along past a later event.
        var events = new List<IAuditEventBase>();
        var first = CreatedAt(new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc), "11111111-1111-1111-1111-111111111111");
        var second = CreatedAt(new DateTime(2025, 2, 1, 0, 0, 0, DateTimeKind.Utc), "22222222-2222-2222-2222-222222222222");
        var third = CreatedAt(new DateTime(2025, 3, 1, 0, 0, 0, DateTimeKind.Utc), "33333333-3333-3333-3333-333333333333");
        Insert(events, first);
        Insert(events, second);
        Insert(events, third);

        Assert.Equal(1, FindInsertionIndex(events, first));
        Assert.Equal(2, FindInsertionIndex(events, second));
        Assert.Equal(3, FindInsertionIndex(events, third));
    }

    private static void AssertOrdered(List<IAuditEventBase> events)
    {
        for (var index = 1; index < events.Count; index++)
            Assert.True(EventDateTimeOf(events[index - 1]) <= EventDateTimeOf(events[index]));
    }

    private static DateTime EventDateTimeOf(IAuditEventBase @event) => ((IEventBase)@event).EventDateTime.Value;

    private static CountryCreatedEvent CreatedAt(DateTime eventDateTime, string eventId) =>
        CountryCreatedEventBuilder.CreateSeed(
            Guid.Parse(eventId),
            Constants.Initialisation.UserID,
            new EventDateTime(eventDateTime),
            new AuditDateTime(eventDateTime),
            "Setup",
            "AA",
            "AAA",
            1,
            "Alpha").Value!;

    private static void Insert(List<IAuditEventBase> events, IAuditEventBase @event)
    {
        var index = FindInsertionIndex(events, @event);
        events.Insert(index, @event);
    }

    private static int FindInsertionIndex(List<IAuditEventBase> events, IAuditEventBase @event) =>
        (int)FindInsertionIndexMethod.Invoke(null, [events, @event])!;

    private static int UpperBound(List<IAuditEventBase> events, DateTime valuationDateTime) =>
        (int)UpperBoundMethod.Invoke(null, [events, valuationDateTime])!;

    private static readonly MethodInfo FindInsertionIndexMethod =
        typeof(InMemoryEventsRepository).GetMethod("FindInsertionIndex", BindingFlags.NonPublic | BindingFlags.Static)
        ?? throw new InvalidOperationException("FindInsertionIndex method not found.");

    private static readonly MethodInfo UpperBoundMethod =
        typeof(InMemoryEventsRepository).GetMethod("UpperBoundByEventDateTime", BindingFlags.NonPublic | BindingFlags.Static)
        ?? throw new InvalidOperationException("UpperBoundByEventDateTime method not found.");
}

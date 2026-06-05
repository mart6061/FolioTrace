using API;
using FolioTrace.Aggregates;
using FolioTrace.Types;

namespace Test;

public sealed class EventHistoryResponseFactoryTests
{
    [Fact]
    public void Create_FiltersOrdersAndAddsApplicationStatus()
    {
        var firstEventID = new EventID(Guid.NewGuid());
        var secondEventID = new EventID(Guid.NewGuid());
        var excludedEventID = new EventID(Guid.NewGuid());
        var events = new[]
        {
            CreateAccountCreatedEvent(excludedEventID, new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)),
            CreateAccountCreatedEvent(secondEventID, new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc)),
            CreateAccountCreatedEvent(firstEventID, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 1, 4, 0, 0, 0, DateTimeKind.Utc))
        };

        var responses = EventHistoryResponseFactory.Create(
                events,
                new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc),
                new DateTime(2026, 1, 3, 0, 0, 0, DateTimeKind.Utc),
                CreateResponse)
            .Cast<EventResponseWithPropertyDetails>()
            .ToList();

        Assert.Collection(
            responses,
            response =>
            {
                Assert.Equal(firstEventID.Value, response.Properties["eventID"]);
                Assert.Equal("omitted", response.Properties["applicationStatus"]);
            },
            response =>
            {
                Assert.Equal(secondEventID.Value, response.Properties["eventID"]);
                Assert.Equal("applied", response.Properties["applicationStatus"]);
            });
        Assert.DoesNotContain(responses, response => Equals(response.Properties["eventID"], excludedEventID.Value));
        Assert.All(responses, response => Assert.DoesNotContain(response.PropertyDetails, detail => detail.Name == "ApplicationStatus"));
    }

    private static EventResponseWithPropertyDetails CreateResponse(AccountCreatedEvent @event) =>
        EventPropertyDetailsFactory.WithPropertyDetails(@event, new
        {
            Type = @event.Type,
            EventID = @event.EventID.Value,
            UserID = @event.UserID.Value,
            EventDateTime = @event.EventDateTime.Value,
            AuditDateTime = @event.AuditDateTime.Value,
            @event.Reason,
            AccountID = @event.AccountID.Value
        });

    private static AccountCreatedEvent CreateAccountCreatedEvent(EventID eventID, DateTime eventDateTime, DateTime auditDateTime)
    {
        var result = AccountCreatedEventBuilder.CreateSeed(
            eventID,
            new UserID(Guid.NewGuid()),
            EventDateTimeBuilder.Create(eventDateTime),
            AuditDateTimeBuilder.Create(auditDateTime),
            "Create account",
            AccountIDBuilder.Create(),
            "Trading",
            "Trading Account",
            Alpha3Builder.Create("GBP"),
            true);

        return result.Value!;
    }
}

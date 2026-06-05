using API;
using FolioTrace.Aggregates;
using FolioTrace.Types;

namespace Test;

public sealed class EventPropertyDetailsFactoryTests
{
    [Fact]
    public void WithPropertyDetails_UsesEventPropertyDescriptionBeforeFormattedName()
    {
        var @event = CreateAccountCreatedEvent();

        var response = EventPropertyDetailsFactory.WithPropertyDetails(@event, new
        {
            Type = @event.Type,
            EventID = @event.EventID.Value,
            BookCurrency = @event.BookCurrency.Value
        });

        var detail = Assert.Single(response.PropertyDetails, detail => detail.Name == "BookCurrency");
        Assert.Equal("BookCurrency", detail.Name);
        Assert.Equal("Book currency", detail.Description);
        Assert.Equal(40, detail.Order);
        Assert.Equal("GBP", detail.Value);
    }

    [Fact]
    public void WithPropertyDetails_FormatsPropertyNamesAndAcronymsWhenNoDescriptionExists()
    {
        var @event = CreateAccountCreatedEvent();

        var response = EventPropertyDetailsFactory.WithPropertyDetails(@event, new
        {
            AccountID = @event.AccountID.Value,
            LEI = "5493001KJTIIGC8Y1R12"
        });

        Assert.Contains(response.PropertyDetails, detail =>
            detail.Name == "AccountID" &&
            detail.Description == "Account ID");
        Assert.Contains(response.PropertyDetails, detail =>
            detail.Name == "LEI" &&
            detail.Description == "LEI");
    }

    [Fact]
    public void WithPropertyDetails_SortsOrderedPropertiesBeforeUnorderedProperties()
    {
        var @event = CreateAccountCreatedEvent();

        var response = EventPropertyDetailsFactory.WithPropertyDetails(@event, new
        {
            Name = @event.Name,
            BookCurrency = @event.BookCurrency.Value,
            Active = @event.Active.Value
        });

        var businessDetails = response.PropertyDetails
            .Where(detail => detail.Name is "Name" or "BookCurrency" or "Active")
            .ToList();

        Assert.Collection(
            businessDetails,
            detail =>
            {
                Assert.Equal("BookCurrency", detail.Name);
                Assert.Equal(40, detail.Order);
            },
            detail => Assert.Equal("Name", detail.Name),
            detail => Assert.Equal("Active", detail.Name));
    }

    [Fact]
    public void WithPropertyDetails_IncludesOrderedCommonEventMetadata()
    {
        var @event = CreateAccountCreatedEvent();

        var response = EventPropertyDetailsFactory.WithPropertyDetails(@event, new
        {
            Type = @event.Type,
            EventID = @event.EventID.Value,
            UserID = @event.UserID.Value,
            EventDateTime = @event.EventDateTime.Value,
            AuditDateTime = @event.AuditDateTime.Value,
            @event.Reason,
            Name = @event.Name
        });

        Assert.Collection(
            response.PropertyDetails.Take(6),
            detail =>
            {
                Assert.Equal("Type", detail.Name);
                Assert.Equal("Event type", detail.Description);
                Assert.Equal(0, detail.Order);
            },
            detail =>
            {
                Assert.Equal("EventID", detail.Name);
                Assert.Equal("Event ID", detail.Description);
                Assert.Equal(10, detail.Order);
            },
            detail =>
            {
                Assert.Equal("UserID", detail.Name);
                Assert.Equal("User ID", detail.Description);
                Assert.Equal(20, detail.Order);
            },
            detail =>
            {
                Assert.Equal("EventDateTime", detail.Name);
                Assert.Equal("Event time", detail.Description);
                Assert.Equal(30, detail.Order);
            },
            detail =>
            {
                Assert.Equal("AuditDateTime", detail.Name);
                Assert.Equal("Audit time", detail.Description);
                Assert.Equal(40, detail.Order);
            },
            detail =>
            {
                Assert.Equal("Reason", detail.Name);
                Assert.Equal("Reason", detail.Description);
                Assert.Equal(50, detail.Order);
            });
    }

    [Fact]
    public void WithPropertyDetails_ExpandsNestedDetails()
    {
        var @event = CreateAccountCreatedEvent();
        var details = new
        {
            BookCurrency = @event.BookCurrency.Value
        };

        var response = EventPropertyDetailsFactory.WithPropertyDetails(@event, new
        {
            Type = @event.Type,
            Details = details
        }, details);

        var detail = Assert.Single(response.PropertyDetails, detail => detail.Name == "BookCurrency");
        Assert.Equal("BookCurrency", detail.Name);
        Assert.Equal("Book currency", detail.Description);
        Assert.Equal("GBP", detail.Value);
    }

    private static AccountCreatedEvent CreateAccountCreatedEvent()
    {
        var result = AccountCreatedEventBuilder.CreateSeed(
            new EventID(Guid.NewGuid()),
            new UserID(Guid.NewGuid()),
            EventDateTimeBuilder.Create(new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc)),
            AuditDateTimeBuilder.Create(new DateTime(2026, 1, 2, 0, 0, 1, DateTimeKind.Utc)),
            "Create account",
            AccountIDBuilder.Create(),
            "Trading",
            "Trading Account",
            Alpha3Builder.Create("GBP"),
            true);

        return result.Value!;
    }
}

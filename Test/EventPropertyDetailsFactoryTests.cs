using API;
using FolioTrace.Aggregates;
using FolioTrace.Types;
using System.Reflection;
using System.Text.Json;

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
        Assert.Equal("Book Currency", detail.Description);
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
            response.PropertyDetails.Take(5),
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
        Assert.DoesNotContain(response.PropertyDetails, detail => detail.Name == "Type");
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
        Assert.Equal("Book Currency", detail.Description);
        Assert.Equal("GBP", detail.Value);
    }

    [Fact]
    public void WithPropertyDetails_SerializesExtensionPropertiesAtTopLevel()
    {
        var @event = CreateAccountCreatedEvent();

        var response = EventPropertyDetailsFactory.WithPropertyDetails(@event, new
        {
            Type = @event.Type,
            EventID = @event.EventID.Value,
            AccountID = @event.AccountID.Value
        });
        response.Properties["applicationStatus"] = "applied";

        using var document = JsonDocument.Parse(JsonSerializer.Serialize(response, new JsonSerializerOptions(JsonSerializerDefaults.Web)));
        var root = document.RootElement;

        Assert.Equal(@event.Type, root.GetProperty("$type").GetString());
        Assert.False(root.TryGetProperty("type", out _));
        Assert.Equal(@event.EventID.Value, root.GetProperty("eventID").GetGuid());
        Assert.Equal("applied", root.GetProperty("applicationStatus").GetString());
        Assert.True(root.TryGetProperty("propertyDetails", out _));
        Assert.False(root.TryGetProperty("properties", out _));
    }

    [Fact]
    public void WithPropertyDetails_SerializesClassDescriptionAtTopLevelOnly()
    {
        var @event = CreateAccountCreatedEvent();

        var response = EventPropertyDetailsFactory.WithPropertyDetails(@event, new
        {
            Type = @event.Type,
            EventID = @event.EventID.Value,
            AccountID = @event.AccountID.Value
        });

        using var document = JsonDocument.Parse(JsonSerializer.Serialize(response, new JsonSerializerOptions(JsonSerializerDefaults.Web)));
        var root = document.RootElement;

        Assert.Equal("Account Created Event", root.GetProperty("classDescription").GetString());
        Assert.DoesNotContain(response.PropertyDetails, detail => detail.Name == "ClassDescription");
        Assert.DoesNotContain(response.PropertyDetails, detail => detail.Name == "classDescription");
    }

    [Fact]
    public void WithPropertyDetails_HandlesDuplicateMetadataFromEventBaseAndInterfaces()
    {
        var @event = CreateTicketCreatedEvent();

        var response = EventPropertyDetailsFactory.WithPropertyDetails(@event, new
        {
            TicketNumber = @event.TicketNumber.Value,
            Side = @event.Side
        });

        var detail = Assert.Single(response.PropertyDetails, detail => detail.Name == "TicketNumber");
        Assert.Equal("Ticket Number", detail.Description);
    }

    [Fact]
    public void WithPropertyDetails_HandlesDuplicateHoldingMetadataFromEventBaseAndInterfaces()
    {
        var @event = CreateHoldingPositionAssetCreatedEvent();

        var response = EventPropertyDetailsFactory.WithPropertyDetails(@event, new
        {
            HoldingID = @event.HoldingID.Value,
            AccountID = @event.AccountID.Value,
            InstrumentID = @event.InstrumentID.Value
        });

        var detail = Assert.Single(response.PropertyDetails, detail => detail.Name == "HoldingID");
        Assert.Equal("Holding ID", detail.Description);
    }

    private static AccountCreatedEvent CreateAccountCreatedEvent()
    {
        var result = AccountCreatedEventBuilder.CreateSeed(
            new EventID(Guid.CreateGuid7()),
            new UserID(Guid.CreateGuid7()),
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

    private static TicketCreatedEvent CreateTicketCreatedEvent() =>
        (TicketCreatedEvent)Activator.CreateInstance(
            typeof(TicketCreatedEvent),
            BindingFlags.Instance | BindingFlags.NonPublic,
            binder: null,
            args:
            [
                new EventID(Guid.CreateGuid7()),
                new UserID(Guid.CreateGuid7()),
                EventDateTimeBuilder.Create(new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc)),
                AuditDateTimeBuilder.Create(new DateTime(2026, 1, 2, 0, 0, 1, DateTimeKind.Utc)),
                "Create ticket",
                new TicketNumber(3),
                TicketSide.Buy,
                InstrumentIDBuilder.Create(),
                Alpha3Builder.Create("GBP")
            ],
            culture: null)!;

    private static HoldingPositionAssetCreatedEvent CreateHoldingPositionAssetCreatedEvent() =>
        (HoldingPositionAssetCreatedEvent)Activator.CreateInstance(
            typeof(HoldingPositionAssetCreatedEvent),
            BindingFlags.Instance | BindingFlags.NonPublic,
            binder: null,
            args:
            [
                new EventID(Guid.CreateGuid7()),
                new UserID(Guid.CreateGuid7()),
                EventDateTimeBuilder.Create(new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc)),
                AuditDateTimeBuilder.Create(new DateTime(2026, 1, 2, 0, 0, 1, DateTimeKind.Utc)),
                "Create holding",
                HoldingIDBuilder.Create(),
                AccountIDBuilder.Create(),
                InstrumentIDBuilder.Create(),
                "Asset holding",
                true,
                false
            ],
            culture: null)!;
}

using FolioTrace.Aggregates;
using FolioTrace.Types;

namespace Test;

public sealed class TicketDetailsTests
{
    private static readonly UserID UserID = new(Guid.Parse("753e051b-f21d-46f4-9643-e13efdfc3615"));
    private static readonly EventDateTime EventDate = EventDateTimeBuilder.Create(new DateTime(2025, 5, 1, 0, 0, 0, DateTimeKind.Utc));
    private static readonly AuditDateTime AuditDate = AuditDateTimeBuilder.Create(new DateTime(2025, 5, 1, 0, 0, 1, DateTimeKind.Utc));
    private static readonly TicketNumber TicketNumber = new(1);

    [Fact]
    public void TicketDetails_IncludesMatchingInstrumentAndAccounts()
    {
        var instrumentID = InstrumentIDBuilder.Create();
        var accountID = AccountIDBuilder.Create();
        var instruments = CreateInstruments(CreateInstrument(instrumentID, "Example Equity"));
        var accounts = CreateAccounts(CreateAccount(accountID, "Trading Account"));
        var created = CreateTicket(instrumentID, instruments);
        var accountAdded = TicketEventBuilder.AddAccount(
            new TicketAccountRequest(UserID, EventDate, "Add account", TicketNumber, accountID),
            new Tickets(EventDate, [created]),
            accounts).Value!;

        var details = new TicketDetails(new Tickets(EventDate, [created, accountAdded]), instruments, accounts);

        var item = Assert.Single(details.Items);
        Assert.Equal("Example Equity", item.Instrument?.Name);
        Assert.Equal("Trading Account", Assert.Single(item.Accounts).Name);
        Assert.Equal(accountID, Assert.Single(item.AccountIDs));
    }

    [Fact]
    public void TicketDetails_MissingReferenceDataIsNonFatal()
    {
        var ticketInstrumentID = InstrumentIDBuilder.Create();
        var ticketAccountID = AccountIDBuilder.Create();
        var originalInstruments = CreateInstruments(CreateInstrument(ticketInstrumentID, "Original Equity"));
        var originalAccounts = CreateAccounts(CreateAccount(ticketAccountID, "Original Account"));
        var missingInstruments = CreateInstruments(CreateInstrument(InstrumentIDBuilder.Create(), "Different Equity"));
        var missingAccounts = CreateAccounts(CreateAccount(AccountIDBuilder.Create(), "Different Account"));
        var created = CreateTicket(ticketInstrumentID, originalInstruments);
        var accountAdded = TicketEventBuilder.AddAccount(
            new TicketAccountRequest(UserID, EventDate, "Add account", TicketNumber, ticketAccountID),
            new Tickets(EventDate, [created]),
            originalAccounts).Value!;

        var details = new TicketDetails(new Tickets(EventDate, [created, accountAdded]), missingInstruments, missingAccounts);

        var item = Assert.Single(details.Items);
        Assert.Null(item.Instrument);
        Assert.Empty(item.Accounts);
        Assert.Equal(ticketInstrumentID, item.InstrumentID);
        Assert.Equal(ticketAccountID, Assert.Single(item.AccountIDs));
    }

    private static TicketCreatedEvent CreateTicket(InstrumentID instrumentID, Instruments instruments) =>
        TicketEventBuilder.Create(
            new TicketCreatedRequest(UserID, EventDate, "Create ticket", TicketSide.Buy, instrumentID),
            TicketNumber,
            instruments).Value!;

    private static InstrumentCreatedEvent CreateInstrument(InstrumentID instrumentID, string name) =>
        InstrumentCreatedEventBuilder.CreateSeed(
            new EventID(Guid.CreateGuid7()),
            UserID,
            EventDate,
            AuditDate,
            "Create instrument",
            instrumentID,
            name,
            $"{name} Formal",
            ExchangeBuilder.Create("XLON"),
            CFIBuilder.Create("ESVUFR"),
            null,
            true,
            Alpha2Builder.Create("GB"),
            Alpha2Builder.Create("GB"),
            Alpha3Builder.Create("GBP")).Value!;

    private static Instruments CreateInstruments(params InstrumentCreatedEvent[] events) =>
        new(EventDate, AuditDate, events.Cast<IInstrumentEvent>().ToList());

    private static AccountCreatedEvent CreateAccount(AccountID accountID, string name) =>
        AccountCreatedEventBuilder.CreateSeed(
            new EventID(Guid.CreateGuid7()),
            UserID,
            EventDate,
            AuditDate,
            "Create account",
            accountID,
            name,
            $"{name} Formal",
            Alpha3Builder.Create("GBP"),
            true).Value!;

    private static Accounts CreateAccounts(params AccountCreatedEvent[] events) =>
        new(EventDate, AuditDate, events.Cast<IAccountEvent>().ToList());
}

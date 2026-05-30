using FolioTrace.Aggregates;
using FolioTrace.Types;

namespace Test;

public sealed class AccountBuilderTests
{
    [Fact]
    public void AccountCreatedEventBuilder_RejectsMissingBookCurrency()
    {
        var currencies = CreateCurrencies("GBP");
        var request = new AccountCreatedRequest(UserID, EventDate, "Create account", null, "Test Account", "Test Account Formal", Alpha3Builder.Create("USD"), true);

        var result = AccountCreatedEventBuilder.Create(request, currencies);

        Assert.False(result.IsValid);
        Assert.Contains("BookCurrency 'USD' does not exist in Currencies.", result.ValidationErrors);
    }

    [Fact]
    public void AccountCreatedEventBuilder_RejectsDuplicateName()
    {
        var currencies = CreateCurrencies("GBP");
        var accounts = CreateAccounts(currencies, "Existing Account");
        var request = new AccountCreatedRequest(UserID, EventDate, "Create account", null, "existing account", "Duplicate Account", Alpha3Builder.Create("GBP"), true);

        var result = AccountCreatedEventBuilder.Create(request, currencies, accounts);

        Assert.False(result.IsValid);
        Assert.Contains("Account Name 'existing account' already exists.", result.ValidationErrors);
    }

    [Fact]
    public void AccountModifiedEventBuilder_RejectsDuplicateNameOnAnotherAccount()
    {
        var currencies = CreateCurrencies("GBP");
        var first = AccountIDBuilder.Create();
        var second = AccountIDBuilder.Create();
        var accounts = CreateAccounts(currencies, ("First Account", first), ("Second Account", second));
        var request = new AccountModifiedRequest(UserID, EventDate, "Modify account", second, "First Account", "Renamed Account");

        var result = AccountModifiedEventBuilder.Create(request, accounts);

        Assert.False(result.IsValid);
        Assert.Contains("Account Name 'First Account' already exists.", result.ValidationErrors);
    }

    [Fact]
    public void AccountActiveModifiedEventBuilder_RejectsMissingAccount()
    {
        var currencies = CreateCurrencies("GBP");
        var accounts = CreateAccounts(currencies, "Existing Account");
        var request = new AccountActiveModifiedRequest(UserID, EventDate, "Deactivate account", AccountIDBuilder.Create(), false);

        var result = AccountActiveModifiedEventBuilder.Create(request, accounts);

        Assert.False(result.IsValid);
        Assert.Contains("No matching Account found for AccountID", result.ValidationErrors[0]);
    }

    [Fact]
    public void Accounts_AppliesCreateModifyAndActiveEvents()
    {
        var currencies = CreateCurrencies("GBP");
        var accountID = AccountIDBuilder.Create();
        var created = AccountCreatedEventBuilder.Create(new AccountCreatedRequest(UserID, EventDate, "Create account", accountID, "Original", "Original Formal", Alpha3Builder.Create("GBP"), true), currencies).Value!;
        var accounts = new Accounts(EventDate, AuditDateTimeBuilder.Create(DateTime.UtcNow), [created]);
        var modified = AccountModifiedEventBuilder.Create(new AccountModifiedRequest(UserID, EventDate, "Modify account", accountID, "Renamed", "Renamed Formal"), accounts).Value!;
        accounts = new Accounts(EventDate, AuditDateTimeBuilder.Create(DateTime.UtcNow), [created, modified]);
        var active = AccountActiveModifiedEventBuilder.Create(new AccountActiveModifiedRequest(UserID, EventDate, "Deactivate account", accountID, false), accounts).Value!;

        var finalAccounts = new Accounts(EventDate, AuditDateTimeBuilder.Create(DateTime.UtcNow), [created, modified, active]);

        var account = Assert.Single(finalAccounts.Items);
        Assert.Equal("Renamed", account.Name);
        Assert.Equal("Renamed Formal", account.FormalName);
        Assert.Equal(Alpha3Builder.Create("GBP"), account.BookCurrency);
        Assert.False(account.Active);
        Assert.Equal(1, account.DisplayOrder.Value);
    }

    [Fact]
    public void AccountDisplayOrderSetEventBuilder_RejectsMissingAccount()
    {
        var currencies = CreateCurrencies("GBP");
        var accounts = CreateAccounts(currencies, "Existing Account");
        var request = new AccountDisplayOrderSetRequest(UserID, EventDate, "Set account order", AccountIDBuilder.Create(), 1);

        var result = AccountDisplayOrderSetEventBuilder.Create(request, accounts);

        Assert.False(result.IsValid);
        Assert.Contains("No matching Account found for AccountID", result.ValidationErrors[0]);
    }

    [Fact]
    public void Accounts_AppliesDisplayOrderSetEvent()
    {
        var currencies = CreateCurrencies("GBP");
        var first = AccountIDBuilder.Create();
        var second = AccountIDBuilder.Create();
        var createdFirst = AccountCreatedEventBuilder.CreateSeed(new EventID(Guid.NewGuid()), UserID, EventDate, AuditDate, "Create account", first, "First", "First Formal", Alpha3Builder.Create("GBP"), true, currencies).Value!;
        var createdSecond = AccountCreatedEventBuilder.CreateSeed(new EventID(Guid.NewGuid()), UserID, EventDate, AuditDateTimeBuilder.Create(AuditDate.Value.AddTicks(1)), "Create account", second, "Second", "Second Formal", Alpha3Builder.Create("GBP"), true, currencies).Value!;
        var accounts = new Accounts(EventDate, AuditDateTimeBuilder.Create(DateTime.UtcNow), [createdFirst, createdSecond]);
        var reorder = AccountDisplayOrderSetEventBuilder.Create(new AccountDisplayOrderSetRequest(UserID, EventDate, "Move first account", first, 3), accounts).Value!;

        var finalAccounts = new Accounts(EventDate, AuditDateTimeBuilder.Create(DateTime.UtcNow), [createdFirst, createdSecond, reorder]);

        Assert.Equal(second, finalAccounts.Items[0].AccountID);
        Assert.Equal(3, finalAccounts.Items[1].DisplayOrder.Value);
    }

    private static readonly UserID UserID = new(Guid.Parse("2aaf4fa2-3d22-4420-90ac-03a028cebbeb"));
    private static readonly EventDateTime EventDate = EventDateTimeBuilder.Create(DateTime.UtcNow.AddMinutes(-10));
    private static readonly AuditDateTime AuditDate = AuditDateTimeBuilder.Create(DateTime.UtcNow.AddMinutes(-9));

    private static Currencies CreateCurrencies(params string[] codes)
    {
        var events = codes.Select((code, index) => CurrencyCreatedEventBuilder.CreateSeed(
            new EventID(Guid.NewGuid()),
            UserID,
            EventDate,
            AuditDateTimeBuilder.Create(AuditDate.Value.AddTicks(index)),
            "Create currency",
            Alpha3Builder.Create(code),
            index,
            2,
            code).Value!).ToList();

        return new Currencies(EventDate, AuditDateTimeBuilder.Create(DateTime.UtcNow), events.Cast<ICurrencyEvent>().ToList());
    }

    private static Accounts CreateAccounts(Currencies currencies, string name) =>
        CreateAccounts(currencies, (name, AccountIDBuilder.Create()));

    private static Accounts CreateAccounts(Currencies currencies, params (string Name, AccountID AccountID)[] seeds)
    {
        var events = seeds.Select((seed, index) => AccountCreatedEventBuilder.CreateSeed(
            new EventID(Guid.NewGuid()),
            UserID,
            EventDate,
            AuditDateTimeBuilder.Create(AuditDate.Value.AddTicks(index)),
            "Create account",
            seed.AccountID,
            seed.Name,
            $"{seed.Name} Formal",
            Alpha3Builder.Create("GBP"),
            true,
            currencies).Value!).ToList();

        return new Accounts(EventDate, AuditDateTimeBuilder.Create(DateTime.UtcNow), events.Cast<IAccountEvent>().ToList());
    }
}

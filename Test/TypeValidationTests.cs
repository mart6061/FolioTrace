using FolioTrace.Aggregates;
using FolioTrace.Types;

namespace Test;

public sealed class TypeValidationTests
{
    public static TheoryData<string> ValidAlpha2Values => new()
    {
        "GB",
        "US"
    };

    public static TheoryData<string?> InvalidAlpha2Values => new()
    {
        null,
        "",
        "G",
        "GBR",
        "gb",
        "G1"
    };

    public static TheoryData<string> ValidAlpha3Values => new()
    {
        "GBR",
        "USA"
    };

    public static TheoryData<string?> InvalidAlpha3Values => new()
    {
        null,
        "",
        "GB",
        "GBRA",
        "gbr",
        "GB1"
    };

    public static TheoryData<string> ValidCfiValues => new()
    {
        "ESVUFR",
        "DBFUFR",
        "OCASPS"
    };

    public static TheoryData<string?> InvalidCfiValues => new()
    {
        null,
        "",
        "ESVUF",
        "ESVUFRX",
        "esvufr",
        "ESVU1R"
    };

    public static TheoryData<string> ValidIsinValues => new()
    {
        "US0378331005",
        "GB0002634946"
    };

    public static TheoryData<string?> InvalidIsinValues => new()
    {
        null,
        "",
        "US037833100",
        "US03783310055",
        "us0378331005",
        "U$0378331005",
        "US0378331006"
    };

    public static TheoryData<string> ValidSedolValues => new()
    {
        "0263494",
        "B1YW440"
    };

    public static TheoryData<string?> InvalidSedolValues => new()
    {
        null,
        "",
        "026349",
        "02634944",
        "b1YW440",
        "B1YW44-"
    };

    public static TheoryData<string> ValidTickerValues => new()
    {
        "MSFT",
        "BRK.B",
        "RDS-A",
        "ABC123"
    };

    public static TheoryData<string?> InvalidTickerValues => new()
    {
        null,
        "",
        "msft",
        "BRK/B",
        "ABCDEFGHIJKLMNOPQRSTU"
    };

    public static TheoryData<string> ValidExchangeValues => new()
    {
        "XLON",
        "XNYS",
        "XNAS",
        "XETR"
    };

    public static TheoryData<string?> InvalidExchangeValues => new()
    {
        null,
        "",
        "X",
        "lower",
        "EXCHANGE-CODE-TOO-LONG"
    };

    [Theory]
    [MemberData(nameof(ValidAlpha2Values))]
    public void Alpha2_AcceptsValidValues(string value)
    {
        var alpha2 = Alpha2Builder.Create(value);

        Assert.Equal(value, alpha2.Value);
    }

    [Theory]
    [MemberData(nameof(InvalidAlpha2Values))]
    public void Alpha2_RejectsInvalidValues(string? value) =>
        Assert.ThrowsAny<ArgumentException>(() => Alpha2Builder.Create(value!));

    [Theory]
    [MemberData(nameof(ValidAlpha3Values))]
    public void Alpha3_AcceptsValidValues(string value)
    {
        var alpha3 = Alpha3Builder.Create(value);

        Assert.Equal(value, alpha3.Value);
    }

    [Theory]
    [MemberData(nameof(InvalidAlpha3Values))]
    public void Alpha3_RejectsInvalidValues(string? value) =>
        Assert.ThrowsAny<ArgumentException>(() => Alpha3Builder.Create(value!));

    [Theory]
    [MemberData(nameof(ValidCfiValues))]
    public void Cfi_AcceptsValidValues(string value)
    {
        var cfi = CFIBuilder.Create(value);

        Assert.Equal(value, cfi.Value);
    }

    [Theory]
    [MemberData(nameof(InvalidCfiValues))]
    public void Cfi_RejectsInvalidValues(string? value) =>
        Assert.ThrowsAny<ArgumentException>(() => CFIBuilder.Create(value!));

    [Fact]
    public void Cfi_ExposesCategoryAndGroup()
    {
        var cfi = CFIBuilder.Create("ESVUFR");

        Assert.Equal('E', cfi.CategoryCode);
        Assert.Equal("Equities", cfi.Category.Name);
        Assert.True(cfi.IsEquity);
        Assert.Equal('S', cfi.GroupCode);
        Assert.Equal("Shares", cfi.Group.Name);
        Assert.Equal('V', cfi.Attribute1);
        Assert.Equal('R', cfi.Attribute4);
        Assert.Equal("ESVUFR", cfi.Value);
        Assert.Equal("Equities", cfi.Category.Name);
        Assert.Equal("Shares", cfi.Group.Name);
    }

    [Fact]
    public void Cfi_AllowsUnknownCategoryAndGroup()
    {
        var cfi = CFIBuilder.Create("ZZZZZZ");

        Assert.Equal("Unknown", cfi.Category.Name);
        Assert.Equal("Unknown", cfi.Group.Name);
        Assert.False(cfi.Category.IsKnown);
        Assert.False(cfi.Group.IsKnown);
    }

    [Fact]
    public void AuditDateTime_AcceptsCurrentUtcValue()
    {
        var auditDateTime = AuditDateTimeBuilder.Create();

        Assert.True(auditDateTime.Value <= DateTime.UtcNow);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("9999-12-31T23:59:59Z")]
    public void AuditDateTime_RejectsInvalidValues(string? value)
    {
        var dateTime = value is null ? default : DateTime.Parse(value, null, System.Globalization.DateTimeStyles.RoundtripKind);

        Assert.Throws<ArgumentException>(() => AuditDateTimeBuilder.Create(dateTime));
    }

    [Fact]
    public void EventDateTime_AcceptsFutureValue()
    {
        var future = DateTime.UtcNow.AddYears(5);

        var eventDateTime = EventDateTimeBuilder.Create(future);

        Assert.Equal(future, eventDateTime.Value);
    }

    [Fact]
    public void EventDateTime_RejectsDefaultValue() =>
        Assert.Throws<ArgumentException>(() => EventDateTimeBuilder.Create(default));

    [Fact]
    public void EventId_AcceptsNonEmptyGuid()
    {
        var value = Guid.CreateGuid7();

        var eventId = new EventID(value);

        Assert.Equal(value, eventId.Value);
    }

    [Fact]
    public void EventId_RejectsEmptyGuid() =>
        Assert.Throws<ArgumentException>(() => new EventID(Guid.Empty));

    [Fact]
    public void EventSetId_AcceptsNonEmptyGuid()
    {
        var eventSetId = EventSetIDBuilder.Create();

        Assert.NotEqual(Guid.Empty, eventSetId.Value);
    }

    [Fact]
    public void EventSetId_RejectsEmptyGuid() =>
        Assert.Throws<ArgumentException>(() => new EventSetID(Guid.Empty));

    [Fact]
    public void AccountId_AcceptsNonEmptyGuid()
    {
        var accountId = AccountIDBuilder.Create();

        Assert.NotEqual(Guid.Empty, accountId.Value);
    }

    [Fact]
    public void AccountId_RejectsEmptyGuid() =>
        Assert.Throws<ArgumentException>(() => new AccountID(Guid.Empty));

    [Fact]
    public void InstrumentId_AcceptsNonEmptyGuid()
    {
        var instrumentId = InstrumentIDBuilder.Create();

        Assert.NotEqual(Guid.Empty, instrumentId.Value);
    }

    [Fact]
    public void InstrumentId_RejectsEmptyGuid() =>
        Assert.Throws<ArgumentException>(() => new InstrumentID(Guid.Empty));

    [Fact]
    public void TransactionQuantity_AcceptsPositiveValues()
    {
        var quantity = new TransactionQuantity(123.12345678m);

        Assert.Equal(123.12345678m, quantity.Value);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(1.123456789)]
    public void TransactionQuantity_RejectsInvalidValues(decimal value) =>
        Assert.Throws<ArgumentException>(() => new TransactionQuantity(value));

    [Fact]
    public void TransactionBookCost_AcceptsZeroAndPositiveValues()
    {
        Assert.Equal(0m, new TransactionBookCost(0m).Value);
        Assert.Equal(123.12345678m, new TransactionBookCost(123.12345678m).Value);
    }

    [Theory]
    [InlineData(-0.00000001)]
    [InlineData(1.123456789)]
    public void TransactionBookCost_RejectsInvalidValues(decimal value) =>
        Assert.Throws<ArgumentException>(() => new TransactionBookCost(value));

    [Fact]
    public void Yield_AcceptsZeroAndPositiveValues()
    {
        Assert.Equal(0m, new Yield(0m).Value);
        Assert.Equal(1.23456789m, new Yield(1.23456789m).Value);
    }

    [Theory]
    [InlineData(-0.00000001)]
    [InlineData(1.123456789)]
    public void Yield_RejectsInvalidValues(decimal value) =>
        Assert.Throws<ArgumentException>(() => new Yield(value));

    [Fact]
    public void Price_RejectsZeroForDirectPriceUsage() =>
        Assert.Throws<ArgumentException>(() => new Price(0m));

    [Fact]
    public void FXQuoteValues_AcceptZero()
    {
        Assert.Equal(0m, new Bid(0m).Value);
        Assert.Equal(0m, new Mid(0m).Value);
        Assert.Equal(0m, new Ask(0m).Value);
    }

    [Fact]
    public void FXQuoteValues_RejectNegativeValues()
    {
        Assert.Throws<ArgumentException>(() => new Bid(-0.00000001m));
        Assert.Throws<ArgumentException>(() => new Mid(-0.00000001m));
        Assert.Throws<ArgumentException>(() => new Ask(-0.00000001m));
    }

    [Fact]
    public void FXQuoteValues_AreAssignableToPrice()
    {
        Price bid = new Bid(1.23m);
        Price mid = new Mid(1.24m);
        Price ask = new Ask(1.25m);

        Assert.Equal(1.23m, bid.Amount);
        Assert.Equal(1.24m, mid.Amount);
        Assert.Equal(1.25m, ask.Amount);
    }

    [Fact]
    public void Price_ConvertsImplicitlyToDecimal()
    {
        Price price = new Bid(1.23456789m);

        decimal value = price;

        Assert.Equal(1.23456789m, value);
    }

    [Fact]
    public void FXPrice_EnforcesBidMidAskOrdering()
    {
        _ = new FXPrice(new Bid(1.00m), new Mid(1.01m), new Ask(1.02m));

        Assert.Throws<ArgumentException>(() => new FXPrice(new Bid(1.02m), new Mid(1.01m), new Ask(1.03m)));
        Assert.Throws<ArgumentException>(() => new FXPrice(new Bid(1.00m), new Mid(1.03m), new Ask(1.02m)));
    }

    [Fact]
    public void InstrumentIncomeCash_FixesIncomeAtZero()
    {
        var income = new InstrumentIncomeCash();

        Assert.Equal(0m, income.Income.Value);
        Assert.Throws<ArgumentException>(() => new InstrumentIncomeCash(new Yield(0.01m)));
    }

    [Fact]
    public void InstrumentPriceCash_FixesPriceAtOne()
    {
        var price = new InstrumentPriceCash();

        Assert.Equal(1m, price.Price.Amount);
        Assert.Throws<ArgumentException>(() => new InstrumentPriceCash(new InstrumentPrice(0.99m)));
    }

    [Theory]
    [MemberData(nameof(ValidInstrumentValuePairs))]
    public void InstrumentValue_AcceptsMatchingPriceAndIncomePairs(IInstrumentPrice price, IInstrumentIncome income)
    {
        var value = CreateInstrumentValue(price, income);

        Assert.Same(price, value.Price);
        Assert.Same(income, value.Income);
    }

    [Theory]
    [MemberData(nameof(InvalidInstrumentValuePairs))]
    public void InstrumentValue_RejectsMismatchedPriceAndIncomePairs(IInstrumentPrice price, IInstrumentIncome income) =>
        Assert.Throws<ArgumentException>(() => CreateInstrumentValue(price, income));

    [Fact]
    public void InstrumentValue_RejectsHalfPresentPriceAndIncomePairs()
    {
        var price = new InstrumentPriceCash();
        var income = new InstrumentIncomeCash();

        Assert.Throws<ArgumentException>(() => CreateInstrumentValue(price, null));
        Assert.Throws<ArgumentException>(() => CreateInstrumentValue(null, income));
    }

    [Fact]
    public void InstrumentValues_OmitsIncompletePriceIncomePairs()
    {
        var instrumentID = InstrumentIDBuilder.Create();
        var eventDateTime = EventDateTimeBuilder.Create(DateTime.UtcNow.AddSeconds(-1));
        var auditDateTime = AuditDateTimeBuilder.Create(DateTime.UtcNow.AddSeconds(-1));
        var instrumentEvent = CreateInstrumentCreatedEvent(instrumentID, eventDateTime, auditDateTime);
        var priceEvent = CreateInstrumentPriceSetEvent(instrumentID, eventDateTime, auditDateTime, new InstrumentPriceCash());

        var values = new InstrumentValues(eventDateTime, [instrumentEvent], [priceEvent], []);
        var value = Assert.Single(values.Items);

        Assert.Null(value.Price);
        Assert.Null(value.PriceValuationDateTime);
        Assert.Null(value.Income);
    }

    [Fact]
    public void InstrumentValues_OmitsMismatchedPriceIncomePairs()
    {
        var instrumentID = InstrumentIDBuilder.Create();
        var eventDateTime = EventDateTimeBuilder.Create(DateTime.UtcNow.AddSeconds(-1));
        var auditDateTime = AuditDateTimeBuilder.Create(DateTime.UtcNow.AddSeconds(-1));
        var instrumentEvent = CreateInstrumentCreatedEvent(instrumentID, eventDateTime, auditDateTime);
        var priceEvent = CreateInstrumentPriceSetEvent(instrumentID, eventDateTime, auditDateTime, new InstrumentPriceCash());
        var incomeEvent = CreateInstrumentIncomeSetEvent(
            instrumentID,
            eventDateTime,
            auditDateTime,
            new InstrumentIncomeEquity(
                new InstrumentPrice(1m),
                "Regular",
                InstrumentDateBuilder.Create(new DateOnly(2026, 1, 1)),
                InstrumentDateBuilder.Create(new DateOnly(2025, 12, 1)),
                InstrumentDateBuilder.Create(new DateOnly(2026, 1, 2)),
                InstrumentDateBuilder.Create(new DateOnly(2026, 1, 31))));

        var values = new InstrumentValues(eventDateTime, [instrumentEvent], [priceEvent], [incomeEvent]);
        var value = Assert.Single(values.Items);

        Assert.Null(value.Price);
        Assert.Null(value.PriceValuationDateTime);
        Assert.Null(value.Income);
    }

    [Theory]
    [MemberData(nameof(ValidExchangeValues))]
    public void Exchange_AcceptsValidValues(string value)
    {
        var exchange = ExchangeBuilder.Create(value);

        Assert.Equal(value, exchange.Value);
    }

    [Theory]
    [MemberData(nameof(InvalidExchangeValues))]
    public void Exchange_RejectsInvalidValues(string? value) =>
        Assert.ThrowsAny<ArgumentException>(() => ExchangeBuilder.Create(value!));

    [Fact]
    public void Money_AddsAndSubtractsSameCurrency()
    {
        var currency = Alpha3Builder.Create("GBP");
        var left = new Money(10.25m, currency);
        var right = new Money(2.50m, currency);

        Assert.Equal(12.75m, (left + right).Amount);
        Assert.Equal(7.75m, (left - right).Amount);
    }

    [Fact]
    public void Money_RejectsCrossCurrencyOperations()
    {
        var sterling = new Money(10m, Alpha3Builder.Create("GBP"));
        var dollars = new Money(10m, Alpha3Builder.Create("USD"));

        Assert.Throws<InvalidOperationException>(() => sterling + dollars);
        Assert.Throws<InvalidOperationException>(() => sterling - dollars);
    }

    [Fact]
    public void InstrumentIdentifier_ValidatesTypedValues()
    {
        var ticker = new InstrumentIdentifier(InstrumentIdentifierType.Ticker, "MSFT");
        var sedol = new InstrumentIdentifier(InstrumentIdentifierType.Sedol, "B1YW440");

        Assert.Equal("MSFT", ticker.Value);
        Assert.Equal("B1YW440", sedol.Value);
        Assert.ThrowsAny<ArgumentException>(() => new InstrumentIdentifier(InstrumentIdentifierType.Ticker, "msft"));
    }

    [Theory]
    [MemberData(nameof(ValidIsinValues))]
    public void Isin_AcceptsValidValues(string value)
    {
        var isin = ISINBuilder.Create(value);

        Assert.Equal(value, isin.Value);
    }

    [Theory]
    [MemberData(nameof(InvalidIsinValues))]
    public void Isin_RejectsInvalidValues(string? value) =>
        Assert.ThrowsAny<ArgumentException>(() => ISINBuilder.Create(value!));

    [Fact]
    public void LastAuditDateTime_AcceptsNonDefaultValue()
    {
        var value = DateTime.UtcNow;

        var lastAuditDateTime = LastAuditDateTimeBuilder.Create(value);

        Assert.Equal(value, lastAuditDateTime.Value);
    }

    [Fact]
    public void LastAuditDateTime_RejectsDefaultValue() =>
        Assert.Throws<ArgumentException>(() => LastAuditDateTimeBuilder.Create(default(DateTime)));

    [Theory]
    [MemberData(nameof(ValidSedolValues))]
    public void Sedol_AcceptsValidValues(string value)
    {
        var sedol = SedolBuilder.Create(value);

        Assert.Equal(value, sedol.Value);
    }

    [Theory]
    [MemberData(nameof(InvalidSedolValues))]
    public void Sedol_RejectsInvalidValues(string? value) =>
        Assert.ThrowsAny<ArgumentException>(() => SedolBuilder.Create(value!));

    [Theory]
    [MemberData(nameof(ValidTickerValues))]
    public void Ticker_AcceptsValidValues(string value)
    {
        var ticker = TickerBuilder.Create(value);

        Assert.Equal(value, ticker.Value);
    }

    [Theory]
    [MemberData(nameof(InvalidTickerValues))]
    public void Ticker_RejectsInvalidValues(string? value) =>
        Assert.ThrowsAny<ArgumentException>(() => TickerBuilder.Create(value!));

    [Fact]
    public void UserId_AcceptsNonEmptyGuid()
    {
        var value = Guid.CreateGuid7();

        var userId = new UserID(value);

        Assert.Equal(value, userId.Value);
    }

    [Fact]
    public void UserId_RejectsEmptyGuid() =>
        Assert.Throws<ArgumentException>(() => new UserID(Guid.Empty));

    public static TheoryData<IInstrumentPrice, IInstrumentIncome> ValidInstrumentValuePairs => new()
    {
        { new InstrumentPriceCash(), new InstrumentIncomeCash() },
        { new InstrumentPriceFixedIncome(new ValuationPrice(100m)), new InstrumentIncomeFixedIncome(new ValuationPrice(1.25m)) },
        {
            new InstrumentPriceEquity(new InstrumentPrice(99m), new InstrumentPrice(100m), new InstrumentPrice(101m), new InstrumentPrice(100m)),
            new InstrumentIncomeEquity(
                new InstrumentPrice(1m),
                "Regular",
                InstrumentDateBuilder.Create(new DateOnly(2026, 1, 1)),
                InstrumentDateBuilder.Create(new DateOnly(2025, 12, 1)),
                InstrumentDateBuilder.Create(new DateOnly(2026, 1, 2)),
                InstrumentDateBuilder.Create(new DateOnly(2026, 1, 31)))
        }
    };

    public static TheoryData<IInstrumentPrice, IInstrumentIncome> InvalidInstrumentValuePairs => new()
    {
        { new InstrumentPriceCash(), new InstrumentIncomeEquity(new InstrumentPrice(1m), "Regular", InstrumentDateBuilder.Create(new DateOnly(2026, 1, 1)), InstrumentDateBuilder.Create(new DateOnly(2025, 12, 1)), InstrumentDateBuilder.Create(new DateOnly(2026, 1, 2)), InstrumentDateBuilder.Create(new DateOnly(2026, 1, 31))) },
        { new InstrumentPriceFixedIncome(new ValuationPrice(100m)), new InstrumentIncomeCash() },
        { new InstrumentPriceEquity(new InstrumentPrice(99m), new InstrumentPrice(100m), new InstrumentPrice(101m), new InstrumentPrice(100m)), new InstrumentIncomeFixedIncome(new ValuationPrice(1.25m)) }
    };

    private static InstrumentValue CreateInstrumentValue(IInstrumentPrice? price, IInstrumentIncome? income)
    {
        var now = DateTime.UtcNow.AddSeconds(-1);
        var eventDateTime = EventDateTimeBuilder.Create(now);
        var auditDateTime = AuditDateTimeBuilder.Create(now);
        var lastAuditDateTime = LastAuditDateTimeBuilder.Create(now);
        var instrument = new Instrument(
            InstrumentIDBuilder.Create(),
            "Test Instrument",
            "Test Instrument plc",
            ExchangeBuilder.Create("XLON"),
            CFIBuilder.Create("ESVUFR"),
            null,
            true,
            Alpha2Builder.Create("GB"),
            Alpha2Builder.Create("GB"),
            Alpha3Builder.Create("GBP"),
            [],
            null,
            eventDateTime,
            auditDateTime,
            new EventID(Guid.CreateGuid7()),
            lastAuditDateTime);

        return new InstrumentValue(
            instrument,
            price,
            price is null ? null : eventDateTime,
            income,
            eventDateTime,
            auditDateTime,
            new EventID(Guid.CreateGuid7()),
            lastAuditDateTime);
    }

    private static InstrumentCreatedEvent CreateInstrumentCreatedEvent(InstrumentID instrumentID, EventDateTime eventDateTime, AuditDateTime auditDateTime) =>
        InstrumentCreatedEventBuilder.CreateSeed(
            new EventID(Guid.CreateGuid7()),
            new UserID(Guid.CreateGuid7()),
            eventDateTime,
            auditDateTime,
            "Create instrument",
            instrumentID,
            "Test Instrument",
            "Test Instrument plc",
            ExchangeBuilder.Create("XLON"),
            CFIBuilder.Create("ESVUFR"),
            null,
            true,
            Alpha2Builder.Create("GB"),
            Alpha2Builder.Create("GB"),
            Alpha3Builder.Create("GBP")).Value!;

    private static InstrumentPriceSetEvent CreateInstrumentPriceSetEvent(InstrumentID instrumentID, EventDateTime eventDateTime, AuditDateTime auditDateTime, IInstrumentPrice price) =>
        InstrumentPriceSetEventBuilder.CreateSeed(
            new EventID(Guid.CreateGuid7()),
            new UserID(Guid.CreateGuid7()),
            eventDateTime,
            auditDateTime,
            "Set instrument price",
            instrumentID,
            price).Value!;

    private static InstrumentIncomeSetEvent CreateInstrumentIncomeSetEvent(InstrumentID instrumentID, EventDateTime eventDateTime, AuditDateTime auditDateTime, IInstrumentIncome income) =>
        InstrumentIncomeSetEventBuilder.CreateSeed(
            new EventID(Guid.CreateGuid7()),
            new UserID(Guid.CreateGuid7()),
            eventDateTime,
            auditDateTime,
            "Set instrument income",
            instrumentID,
            income).Value!;
}

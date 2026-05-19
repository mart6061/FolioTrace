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
        var value = Guid.NewGuid();

        var eventId = new EventID(value);

        Assert.Equal(value, eventId.Value);
    }

    [Fact]
    public void EventId_RejectsEmptyGuid() =>
        Assert.Throws<ArgumentException>(() => new EventID(Guid.Empty));

    [Fact]
    public void InstrumentId_AcceptsNonEmptyGuid()
    {
        var instrumentId = InstrumentIDBuilder.Create();

        Assert.NotEqual(Guid.Empty, instrumentId.Value);
    }

    [Fact]
    public void InstrumentId_RejectsEmptyGuid() =>
        Assert.Throws<ArgumentException>(() => new InstrumentID(Guid.Empty));

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
        var value = Guid.NewGuid();

        var userId = new UserID(value);

        Assert.Equal(value, userId.Value);
    }

    [Fact]
    public void UserId_RejectsEmptyGuid() =>
        Assert.Throws<ArgumentException>(() => new UserID(Guid.Empty));
}

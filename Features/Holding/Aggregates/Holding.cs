using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(HoldingPositionMemo), nameof(HoldingPositionMemo))]
[JsonDerivedType(typeof(HoldingPositionCash), nameof(HoldingPositionCash))]
[JsonDerivedType(typeof(HoldingCashDebt), nameof(HoldingCashDebt))]
[JsonDerivedType(typeof(HoldingCashInvestable), nameof(HoldingCashInvestable))]
[JsonDerivedType(typeof(HoldingCashNonInvestable), nameof(HoldingCashNonInvestable))]
[JsonDerivedType(typeof(HoldingInflow), nameof(HoldingInflow))]
[JsonDerivedType(typeof(HoldingOutflow), nameof(HoldingOutflow))]
[JsonDerivedType(typeof(HoldingInspecieIn), nameof(HoldingInspecieIn))]
[JsonDerivedType(typeof(HoldingInspecieOut), nameof(HoldingInspecieOut))]
[JsonDerivedType(typeof(HoldingFeesCustodian), nameof(HoldingFeesCustodian))]
[JsonDerivedType(typeof(HoldingFeesAdministrator), nameof(HoldingFeesAdministrator))]
[JsonDerivedType(typeof(HoldingFeesBank), nameof(HoldingFeesBank))]
[JsonDerivedType(typeof(HoldingIncome), nameof(HoldingIncome))]
[JsonDerivedType(typeof(HoldingInterest), nameof(HoldingInterest))]
public abstract record Holding : IModel
{
    public required HoldingID HoldingID { get; init; }
    public required AccountID AccountID { get; init; }
    public required InstrumentID InstrumentID { get; init; }
    public required string Name { get; init; }
    public required Active Active { get; init; }
    public required bool Default { get; init; }
    public bool IncludeInValuation => this is IHoldingPosition;
    public required EventDateTime ValuationDateTime { get; init; }
    public required AuditDateTime AsOfDateTime { get; init; }
    public required EventID LastEventID { get; init; }
    public required LastAuditDateTime LastAuditDateTime { get; init; }

    [SetsRequiredMembers]
    protected Holding(HoldingID holdingID, AccountID accountID, InstrumentID instrumentID, string name, Active active, bool isDefault, EventDateTime valuationDateTime, AuditDateTime asOfDateTime, EventID lastEventID, LastAuditDateTime lastAuditDateTime)
    {
        HoldingID = holdingID ?? throw new ArgumentNullException(nameof(holdingID));
        AccountID = accountID ?? throw new ArgumentNullException(nameof(accountID));
        InstrumentID = instrumentID ?? throw new ArgumentNullException(nameof(instrumentID));
        Name = name?.Trim() ?? string.Empty;
        Active = active;
        Default = isDefault;
        ValuationDateTime = valuationDateTime ?? throw new ArgumentNullException(nameof(valuationDateTime));
        AsOfDateTime = asOfDateTime ?? throw new ArgumentNullException(nameof(asOfDateTime));
        LastEventID = lastEventID ?? throw new ArgumentNullException(nameof(lastEventID));
        LastAuditDateTime = lastAuditDateTime ?? throw new ArgumentNullException(nameof(lastAuditDateTime));
    }

    public virtual string ToData() => $"{HoldingID.ToData()}|{AccountID.ToData()}|{InstrumentID.ToData()}|{this.GetHoldingKindName()}|{Name}|{Active}|{Default}|{ValuationDateTime.ToData()}|{AsOfDateTime.ToData()}|{LastEventID.ToData()}|{LastAuditDateTime.ToData()}";

    public virtual string ToDetail() => $"{nameof(Holding)}: {Name} ({HoldingID}, AccountID: {AccountID}, InstrumentID: {InstrumentID}, HoldingKind: {this.GetHoldingKindName()}, Active: {Active}, Default: {Default}, IncludeInValuation: {IncludeInValuation})";
}

public interface IHoldingPosition;

public interface IHoldingNominal;

public static class HoldingKindRuntime
{
    public static string GetHoldingKindName(this Holding holding) =>
        GetKindName(holding?.GetType() ?? throw new ArgumentNullException(nameof(holding)));

    public static string GetHoldingKindName(this HoldingCreatedEvent holdingEvent) =>
        GetEventKindName(holdingEvent?.GetType() ?? throw new ArgumentNullException(nameof(holdingEvent)), "CreatedEvent");

    public static string GetHoldingKindName(this HoldingModifiedEvent holdingEvent) =>
        GetEventKindName(holdingEvent?.GetType() ?? throw new ArgumentNullException(nameof(holdingEvent)), "ModifiedEvent");

    public static string GetKindName<T>() => GetKindName(typeof(T));

    public static string GetKindName(Type type)
    {
        if (type is null)
            throw new ArgumentNullException(nameof(type));

        return type.Name.StartsWith("Holding", StringComparison.Ordinal)
            ? type.Name["Holding".Length..]
            : type.Name;
    }

    public static bool IsPositionCash<T>() => typeof(T) == typeof(HoldingPositionCash);

    public static bool IsPositionMemo<T>() => typeof(T) == typeof(HoldingPositionMemo);

    private static string GetEventKindName(Type type, string suffix)
    {
        var name = GetKindName(type);
        return name.EndsWith(suffix, StringComparison.Ordinal)
            ? name[..^suffix.Length]
            : name;
    }
}

public sealed record HoldingPositionMemo : Holding, IHoldingPosition
{
    [JsonConstructor]
    [SetsRequiredMembers]
    public HoldingPositionMemo(HoldingID holdingID, AccountID accountID, InstrumentID instrumentID, string name, Active active, bool isDefault, EventDateTime valuationDateTime, AuditDateTime asOfDateTime, EventID lastEventID, LastAuditDateTime lastAuditDateTime)
        : base(holdingID, accountID, instrumentID, name, active, isDefault, valuationDateTime, asOfDateTime, lastEventID, lastAuditDateTime)
    {
    }
}

public sealed record HoldingPositionCash : Holding, IHoldingPosition
{
    [JsonConstructor]
    [SetsRequiredMembers]
    public HoldingPositionCash(HoldingID holdingID, AccountID accountID, InstrumentID instrumentID, string name, Active active, bool isDefault, EventDateTime valuationDateTime, AuditDateTime asOfDateTime, EventID lastEventID, LastAuditDateTime lastAuditDateTime)
        : base(holdingID, accountID, instrumentID, name, active, isDefault, valuationDateTime, asOfDateTime, lastEventID, lastAuditDateTime)
    {
    }
}

public abstract record HoldingBank : Holding
{
    public required string BankName { get; init; }
    public required string AccountName { get; init; }
    public required SortCode SortCode { get; init; }
    public required BankAccountNumber AccountNumber { get; init; }
    public required BIC BIC { get; init; }
    public required IBAN IBAN { get; init; }

    [SetsRequiredMembers]
    protected HoldingBank(HoldingID holdingID, AccountID accountID, InstrumentID instrumentID, string name, Active active, bool isDefault, EventDateTime valuationDateTime, AuditDateTime asOfDateTime, EventID lastEventID, LastAuditDateTime lastAuditDateTime, string bankName, string accountName, SortCode sortCode, BankAccountNumber accountNumber, BIC bic, IBAN iban)
        : base(holdingID, accountID, instrumentID, name, active, isDefault, valuationDateTime, asOfDateTime, lastEventID, lastAuditDateTime)
    {
        BankName = bankName?.Trim() ?? string.Empty;
        AccountName = accountName?.Trim() ?? string.Empty;
        SortCode = sortCode ?? throw new ArgumentNullException(nameof(sortCode));
        AccountNumber = accountNumber ?? throw new ArgumentNullException(nameof(accountNumber));
        BIC = bic ?? throw new ArgumentNullException(nameof(bic));
        IBAN = iban ?? throw new ArgumentNullException(nameof(iban));
    }

    public override string ToData() => $"{base.ToData()}|{BankName}|{AccountName}|{SortCode.ToData()}|{AccountNumber.ToData()}|{BIC.ToData()}|{IBAN.ToData()}";

    public override string ToDetail() => $"{base.ToDetail()} BankName: {BankName}, AccountName: {AccountName}, SortCode: {SortCode}, AccountNumber: {AccountNumber}, BIC: {BIC}, IBAN: {IBAN}";
}

public sealed record HoldingCashDebt : HoldingBank, IHoldingPosition
{
    [JsonConstructor]
    [SetsRequiredMembers]
    public HoldingCashDebt(HoldingID holdingID, AccountID accountID, InstrumentID instrumentID, string name, Active active, bool isDefault, EventDateTime valuationDateTime, AuditDateTime asOfDateTime, EventID lastEventID, LastAuditDateTime lastAuditDateTime, string bankName, string accountName, SortCode sortCode, BankAccountNumber accountNumber, BIC bic, IBAN iban)
        : base(holdingID, accountID, instrumentID, name, active, isDefault, valuationDateTime, asOfDateTime, lastEventID, lastAuditDateTime, bankName, accountName, sortCode, accountNumber, bic, iban)
    {
    }
}

public sealed record HoldingCashInvestable : HoldingBank, IHoldingPosition
{
    [JsonConstructor]
    [SetsRequiredMembers]
    public HoldingCashInvestable(HoldingID holdingID, AccountID accountID, InstrumentID instrumentID, string name, Active active, bool isDefault, EventDateTime valuationDateTime, AuditDateTime asOfDateTime, EventID lastEventID, LastAuditDateTime lastAuditDateTime, string bankName, string accountName, SortCode sortCode, BankAccountNumber accountNumber, BIC bic, IBAN iban)
        : base(holdingID, accountID, instrumentID, name, active, isDefault, valuationDateTime, asOfDateTime, lastEventID, lastAuditDateTime, bankName, accountName, sortCode, accountNumber, bic, iban)
    {
    }
}

public sealed record HoldingCashNonInvestable : HoldingBank, IHoldingPosition
{
    [JsonConstructor]
    [SetsRequiredMembers]
    public HoldingCashNonInvestable(HoldingID holdingID, AccountID accountID, InstrumentID instrumentID, string name, Active active, bool isDefault, EventDateTime valuationDateTime, AuditDateTime asOfDateTime, EventID lastEventID, LastAuditDateTime lastAuditDateTime, string bankName, string accountName, SortCode sortCode, BankAccountNumber accountNumber, BIC bic, IBAN iban)
        : base(holdingID, accountID, instrumentID, name, active, isDefault, valuationDateTime, asOfDateTime, lastEventID, lastAuditDateTime, bankName, accountName, sortCode, accountNumber, bic, iban)
    {
    }
}

public sealed record HoldingInflow : Holding, IHoldingNominal
{
    [JsonConstructor]
    [SetsRequiredMembers]
    public HoldingInflow(HoldingID holdingID, AccountID accountID, InstrumentID instrumentID, string name, Active active, bool isDefault, EventDateTime valuationDateTime, AuditDateTime asOfDateTime, EventID lastEventID, LastAuditDateTime lastAuditDateTime)
        : base(holdingID, accountID, instrumentID, name, active, isDefault, valuationDateTime, asOfDateTime, lastEventID, lastAuditDateTime)
    {
    }
}

public sealed record HoldingOutflow : Holding, IHoldingNominal
{
    [JsonConstructor]
    [SetsRequiredMembers]
    public HoldingOutflow(HoldingID holdingID, AccountID accountID, InstrumentID instrumentID, string name, Active active, bool isDefault, EventDateTime valuationDateTime, AuditDateTime asOfDateTime, EventID lastEventID, LastAuditDateTime lastAuditDateTime)
        : base(holdingID, accountID, instrumentID, name, active, isDefault, valuationDateTime, asOfDateTime, lastEventID, lastAuditDateTime)
    {
    }
}

public sealed record HoldingInspecieIn : Holding, IHoldingNominal
{
    [JsonConstructor]
    [SetsRequiredMembers]
    public HoldingInspecieIn(HoldingID holdingID, AccountID accountID, InstrumentID instrumentID, string name, Active active, bool isDefault, EventDateTime valuationDateTime, AuditDateTime asOfDateTime, EventID lastEventID, LastAuditDateTime lastAuditDateTime)
        : base(holdingID, accountID, instrumentID, name, active, isDefault, valuationDateTime, asOfDateTime, lastEventID, lastAuditDateTime)
    {
    }
}

public sealed record HoldingInspecieOut : Holding, IHoldingNominal
{
    [JsonConstructor]
    [SetsRequiredMembers]
    public HoldingInspecieOut(HoldingID holdingID, AccountID accountID, InstrumentID instrumentID, string name, Active active, bool isDefault, EventDateTime valuationDateTime, AuditDateTime asOfDateTime, EventID lastEventID, LastAuditDateTime lastAuditDateTime)
        : base(holdingID, accountID, instrumentID, name, active, isDefault, valuationDateTime, asOfDateTime, lastEventID, lastAuditDateTime)
    {
    }
}

public abstract record HoldingFees : Holding, IHoldingNominal
{
    [SetsRequiredMembers]
    protected HoldingFees(HoldingID holdingID, AccountID accountID, InstrumentID instrumentID, string name, Active active, bool isDefault, EventDateTime valuationDateTime, AuditDateTime asOfDateTime, EventID lastEventID, LastAuditDateTime lastAuditDateTime)
        : base(holdingID, accountID, instrumentID, name, active, isDefault, valuationDateTime, asOfDateTime, lastEventID, lastAuditDateTime)
    {
    }
}

public sealed record HoldingFeesCustodian : HoldingFees
{
    [JsonConstructor]
    [SetsRequiredMembers]
    public HoldingFeesCustodian(HoldingID holdingID, AccountID accountID, InstrumentID instrumentID, string name, Active active, bool isDefault, EventDateTime valuationDateTime, AuditDateTime asOfDateTime, EventID lastEventID, LastAuditDateTime lastAuditDateTime)
        : base(holdingID, accountID, instrumentID, name, active, isDefault, valuationDateTime, asOfDateTime, lastEventID, lastAuditDateTime)
    {
    }
}

public sealed record HoldingFeesAdministrator : HoldingFees
{
    [JsonConstructor]
    [SetsRequiredMembers]
    public HoldingFeesAdministrator(HoldingID holdingID, AccountID accountID, InstrumentID instrumentID, string name, Active active, bool isDefault, EventDateTime valuationDateTime, AuditDateTime asOfDateTime, EventID lastEventID, LastAuditDateTime lastAuditDateTime)
        : base(holdingID, accountID, instrumentID, name, active, isDefault, valuationDateTime, asOfDateTime, lastEventID, lastAuditDateTime)
    {
    }
}

public sealed record HoldingFeesBank : HoldingFees
{
    [JsonConstructor]
    [SetsRequiredMembers]
    public HoldingFeesBank(HoldingID holdingID, AccountID accountID, InstrumentID instrumentID, string name, Active active, bool isDefault, EventDateTime valuationDateTime, AuditDateTime asOfDateTime, EventID lastEventID, LastAuditDateTime lastAuditDateTime)
        : base(holdingID, accountID, instrumentID, name, active, isDefault, valuationDateTime, asOfDateTime, lastEventID, lastAuditDateTime)
    {
    }
}

public sealed record HoldingIncome : Holding, IHoldingNominal
{
    [JsonConstructor]
    [SetsRequiredMembers]
    public HoldingIncome(HoldingID holdingID, AccountID accountID, InstrumentID instrumentID, string name, Active active, bool isDefault, EventDateTime valuationDateTime, AuditDateTime asOfDateTime, EventID lastEventID, LastAuditDateTime lastAuditDateTime)
        : base(holdingID, accountID, instrumentID, name, active, isDefault, valuationDateTime, asOfDateTime, lastEventID, lastAuditDateTime)
    {
    }
}

public sealed record HoldingInterest : Holding, IHoldingNominal
{
    [JsonConstructor]
    [SetsRequiredMembers]
    public HoldingInterest(HoldingID holdingID, AccountID accountID, InstrumentID instrumentID, string name, Active active, bool isDefault, EventDateTime valuationDateTime, AuditDateTime asOfDateTime, EventID lastEventID, LastAuditDateTime lastAuditDateTime)
        : base(holdingID, accountID, instrumentID, name, active, isDefault, valuationDateTime, asOfDateTime, lastEventID, lastAuditDateTime)
    {
    }
}

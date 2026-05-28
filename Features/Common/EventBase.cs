using System.Text.Json.Serialization;
using FolioTrace.Aggregates;
using FolioTrace.Types;

namespace FolioTrace.Common;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(AccountCreatedEvent), nameof(AccountCreatedEvent))]
[JsonDerivedType(typeof(AccountModifiedEvent), nameof(AccountModifiedEvent))]
[JsonDerivedType(typeof(AccountActiveModifiedEvent), nameof(AccountActiveModifiedEvent))]
[JsonDerivedType(typeof(CountryCreatedEvent), nameof(CountryCreatedEvent))]
[JsonDerivedType(typeof(CountryModifiedEvent), nameof(CountryModifiedEvent))]
[JsonDerivedType(typeof(CountryFlagModifiedEvent), nameof(CountryFlagModifiedEvent))]
[JsonDerivedType(typeof(CurrencyCreatedEvent), nameof(CurrencyCreatedEvent))]
[JsonDerivedType(typeof(CurrencyModifiedEvent), nameof(CurrencyModifiedEvent))]
[JsonDerivedType(typeof(FXCreatedEvent), nameof(FXCreatedEvent))]
[JsonDerivedType(typeof(FXActiveModifiedEvent), nameof(FXActiveModifiedEvent))]
[JsonDerivedType(typeof(FXRateSetEvent), nameof(FXRateSetEvent))]
[JsonDerivedType(typeof(HoldingPositionMemoCreatedEvent), nameof(HoldingPositionMemoCreatedEvent))]
[JsonDerivedType(typeof(HoldingPositionCashCreatedEvent), nameof(HoldingPositionCashCreatedEvent))]
[JsonDerivedType(typeof(HoldingCashDebtCreatedEvent), nameof(HoldingCashDebtCreatedEvent))]
[JsonDerivedType(typeof(HoldingCashInvestableCreatedEvent), nameof(HoldingCashInvestableCreatedEvent))]
[JsonDerivedType(typeof(HoldingCashNonInvestableCreatedEvent), nameof(HoldingCashNonInvestableCreatedEvent))]
[JsonDerivedType(typeof(HoldingInflowCreatedEvent), nameof(HoldingInflowCreatedEvent))]
[JsonDerivedType(typeof(HoldingOutflowCreatedEvent), nameof(HoldingOutflowCreatedEvent))]
[JsonDerivedType(typeof(HoldingInspecieInCreatedEvent), nameof(HoldingInspecieInCreatedEvent))]
[JsonDerivedType(typeof(HoldingInspecieOutCreatedEvent), nameof(HoldingInspecieOutCreatedEvent))]
[JsonDerivedType(typeof(HoldingFeesCustodianCreatedEvent), nameof(HoldingFeesCustodianCreatedEvent))]
[JsonDerivedType(typeof(HoldingFeesAdministratorCreatedEvent), nameof(HoldingFeesAdministratorCreatedEvent))]
[JsonDerivedType(typeof(HoldingFeesBankCreatedEvent), nameof(HoldingFeesBankCreatedEvent))]
[JsonDerivedType(typeof(HoldingIncomeCreatedEvent), nameof(HoldingIncomeCreatedEvent))]
[JsonDerivedType(typeof(HoldingInterestCreatedEvent), nameof(HoldingInterestCreatedEvent))]
[JsonDerivedType(typeof(HoldingPositionMemoModifiedEvent), nameof(HoldingPositionMemoModifiedEvent))]
[JsonDerivedType(typeof(HoldingPositionCashModifiedEvent), nameof(HoldingPositionCashModifiedEvent))]
[JsonDerivedType(typeof(HoldingCashDebtModifiedEvent), nameof(HoldingCashDebtModifiedEvent))]
[JsonDerivedType(typeof(HoldingCashInvestableModifiedEvent), nameof(HoldingCashInvestableModifiedEvent))]
[JsonDerivedType(typeof(HoldingCashNonInvestableModifiedEvent), nameof(HoldingCashNonInvestableModifiedEvent))]
[JsonDerivedType(typeof(HoldingInflowModifiedEvent), nameof(HoldingInflowModifiedEvent))]
[JsonDerivedType(typeof(HoldingOutflowModifiedEvent), nameof(HoldingOutflowModifiedEvent))]
[JsonDerivedType(typeof(HoldingInspecieInModifiedEvent), nameof(HoldingInspecieInModifiedEvent))]
[JsonDerivedType(typeof(HoldingInspecieOutModifiedEvent), nameof(HoldingInspecieOutModifiedEvent))]
[JsonDerivedType(typeof(HoldingFeesCustodianModifiedEvent), nameof(HoldingFeesCustodianModifiedEvent))]
[JsonDerivedType(typeof(HoldingFeesAdministratorModifiedEvent), nameof(HoldingFeesAdministratorModifiedEvent))]
[JsonDerivedType(typeof(HoldingFeesBankModifiedEvent), nameof(HoldingFeesBankModifiedEvent))]
[JsonDerivedType(typeof(HoldingIncomeModifiedEvent), nameof(HoldingIncomeModifiedEvent))]
[JsonDerivedType(typeof(HoldingInterestModifiedEvent), nameof(HoldingInterestModifiedEvent))]
[JsonDerivedType(typeof(HoldingActiveModifiedEvent), nameof(HoldingActiveModifiedEvent))]
[JsonDerivedType(typeof(InstrumentCreatedEvent), nameof(InstrumentCreatedEvent))]
[JsonDerivedType(typeof(InstrumentModifiedEvent), nameof(InstrumentModifiedEvent))]
[JsonDerivedType(typeof(InstrumentActiveModifiedEvent), nameof(InstrumentActiveModifiedEvent))]
[JsonDerivedType(typeof(InstrumentIdentifierSetEvent), nameof(InstrumentIdentifierSetEvent))]
[JsonDerivedType(typeof(InstrumentIdentifierUnsetEvent), nameof(InstrumentIdentifierUnsetEvent))]
[JsonDerivedType(typeof(InstrumentTermsSetEvent), nameof(InstrumentTermsSetEvent))]
[JsonDerivedType(typeof(InstrumentPriceSetEvent), nameof(InstrumentPriceSetEvent))]
[JsonDerivedType(typeof(InstrumentIncomeSetEvent), nameof(InstrumentIncomeSetEvent))]
[JsonDerivedType(typeof(TransactionCreditEvent), nameof(TransactionCreditEvent))]
[JsonDerivedType(typeof(TransactionDebitEvent), nameof(TransactionDebitEvent))]
[JsonDerivedType(typeof(TransactionCancellationEvent), nameof(TransactionCancellationEvent))]
[JsonDerivedType(typeof(UserCreatedEvent), nameof(UserCreatedEvent))]
[JsonDerivedType(typeof(UserModifiedEvent), nameof(UserModifiedEvent))]
public abstract record EventBase(EventID EventID, UserID UserID, EventDateTime EventDateTime, AuditDateTime AuditDateTime, string Reason) : IEventBase
{
    [JsonIgnore]
    public Guid Id => EventID.Value;

    [JsonPropertyName("$type")]
    public abstract string Type { get; }

    public virtual string ToData() => $"{EventID.ToData()}|{UserID.ToData()}|{EventDateTime.ToData()}|{AuditDateTime.ToData()}|{Reason}";

    public  virtual string ToDetail() => $"{nameof(EventBase)}: ({EventID.ToDetail()}, {UserID.ToDetail()}, {EventDateTime.ToDetail()}, {AuditDateTime.ToDetail()}, Reason: {Reason})";
}

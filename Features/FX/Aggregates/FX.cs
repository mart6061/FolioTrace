using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record FX : IModel
{
    public required CurrencyPair Pair { get; init; }

    public required Alpha3 BaseCurrency { get; init; }

    public required Alpha3 QuoteCurrency { get; init; }

    public required string DisplayPair { get; init; }

    public required bool Active { get; init; }

    public required EventDateTime ValuationDateTime { get; init; }

    public required AuditDateTime AsOfDateTime { get; init; }

    public required EventID LastEventID { get; init; }

    public required LastAuditDateTime LastAuditDateTime { get; init; }

    [JsonConstructor]
    [SetsRequiredMembers]
    public FX(CurrencyPair pair, Alpha3 baseCurrency, Alpha3 quoteCurrency, string displayPair, bool active, EventDateTime valuationDateTime, AuditDateTime asOfDateTime, EventID lastEventID, LastAuditDateTime lastAuditDateTime)
    {
        Pair = pair;
        BaseCurrency = baseCurrency;
        QuoteCurrency = quoteCurrency;
        DisplayPair = displayPair;
        Active = active;
        ValuationDateTime = valuationDateTime;
        AsOfDateTime = asOfDateTime;
        LastEventID = lastEventID;
        LastAuditDateTime = lastAuditDateTime;
    }

    public string ToData() => $"{Pair.ToData()}|{BaseCurrency.ToData()}|{QuoteCurrency.ToData()}|{Active}|{ValuationDateTime.ToData()}|{AsOfDateTime.ToData()}|{LastEventID.ToData()}|{LastAuditDateTime.ToData()}";

    public string ToDetail() => $"{nameof(FX)}: (Pair: {Pair.ToDetail()}, Active: {Active}, ValuationDateTime: {ValuationDateTime.ToDetail()}, AsOfDateTime: {AsOfDateTime.ToDetail()}, LastEventID: {LastEventID.ToDetail()}, LastAuditDateTime: {LastAuditDateTime.ToDetail()})";
}

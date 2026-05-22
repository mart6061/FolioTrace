using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record InstrumentCreatedRequest(
    UserID UserID,
    EventDateTime EventDateTime,
    string Reason,
    InstrumentID? InstrumentID,
    string Name,
    string FormalName,
    Exchange Exchange,
    CFI CFI,
    InstrumentLogo? Logo,
    bool Active,
    Alpha2 IncomeCountry,
    Alpha2 PriceCountry) : IEventRequest;

public sealed record InstrumentModifiedRequest(
    UserID UserID,
    EventDateTime EventDateTime,
    string Reason,
    InstrumentID InstrumentID,
    string Name,
    string FormalName,
    Exchange Exchange,
    CFI CFI,
    InstrumentLogo? Logo,
    Alpha2 IncomeCountry,
    Alpha2 PriceCountry) : IEventRequest;

public sealed record InstrumentActiveModifiedRequest(
    UserID UserID,
    EventDateTime EventDateTime,
    string Reason,
    InstrumentID InstrumentID,
    bool Active) : IEventRequest;

public sealed record InstrumentIdentifierSetRequest(
    UserID UserID,
    EventDateTime EventDateTime,
    string Reason,
    InstrumentID InstrumentID,
    InstrumentIdentifier Identifier) : IEventRequest;

public sealed record InstrumentIdentifierUnsetRequest(
    UserID UserID,
    EventDateTime EventDateTime,
    string Reason,
    InstrumentID InstrumentID,
    InstrumentIdentifierType IdentifierType) : IEventRequest;

public sealed record InstrumentTermsSetRequest(
    UserID UserID,
    EventDateTime EventDateTime,
    string Reason,
    InstrumentID InstrumentID,
    IInstrumentTerms Terms) : IEventRequest;

public sealed record InstrumentPriceSetRequest(
    UserID UserID,
    EventDateTime EventDateTime,
    string Reason,
    InstrumentID InstrumentID,
    IInstrumentPrice Price) : IEventRequest;

public sealed record InstrumentIncomeSetRequest(
    UserID UserID,
    EventDateTime EventDateTime,
    string Reason,
    InstrumentID InstrumentID,
    IInstrumentIncome Income) : IEventRequest;

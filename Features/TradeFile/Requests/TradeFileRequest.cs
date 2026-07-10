using FolioTrace.Common;
using FolioTrace.Types;
namespace FolioTrace.Aggregates;
public sealed record TradeFileRequest(UserID UserID, EventDateTime EventDateTime, string Reason, LegalEntityIdentifier BrokerLEI, List<TicketNumber> TicketNumbers) : IEventRequest;

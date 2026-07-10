using FolioTrace.Common;
using FolioTrace.Types;
namespace FolioTrace.Aggregates;
public sealed record AccountIdentifierUnsetRequest(UserID UserID, EventDateTime EventDateTime, string Reason, AccountID AccountID, InstrumentIdentifierType IdentifierType) : IEventRequest;

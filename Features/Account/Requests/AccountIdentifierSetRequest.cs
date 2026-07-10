using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record AccountIdentifierSetRequest(UserID UserID, EventDateTime EventDateTime, string Reason, AccountID AccountID, InstrumentIdentifier Identifier) : IEventRequest;

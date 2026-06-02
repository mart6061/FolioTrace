using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record UserSessionEventRequest(UserID UserID, DateTime EventDateTime, string Reason);

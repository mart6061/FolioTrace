using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record UserMenuPreferencesRequest(
    UserID UserID,
    EventDateTime EventDateTime,
    string Reason,
    IReadOnlyList<UserMenuPreferenceItem> Items) : IEventRequest;

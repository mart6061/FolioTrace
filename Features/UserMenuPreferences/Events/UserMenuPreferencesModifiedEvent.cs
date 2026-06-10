using System.Text.Json.Serialization;
using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

[EventClass(EventType = EventClassTypeEnum.Modified, Description = "User Menu Preferences Modified Event")]
public sealed record UserMenuPreferencesModifiedEvent : EventBase, IUserMenuPreferencesEvent
{
    [EventProperty(Description = "Items")]
    public IReadOnlyList<UserMenuPreferenceItem> Items { get; init; } = [];

    [JsonConstructor]
    private UserMenuPreferencesModifiedEvent()
        : base(null!, null!, null!, null!, string.Empty)
    {
    }

    internal UserMenuPreferencesModifiedEvent(EventID eventID, UserID userID, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, IReadOnlyList<UserMenuPreferenceItem> items)
        : base(eventID, userID, eventDateTime, auditDateTime, reason)
    {
        Items = UserMenuPreferenceDefaults.Normalize(items);
    }

    public override string Type => nameof(UserMenuPreferencesModifiedEvent);
}

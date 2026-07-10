using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record InputControlSettingsRequest(
    UserID UserID,
    EventDateTime EventDateTime,
    string Reason,
    IReadOnlyList<InputControlSettingDefinition> Settings);

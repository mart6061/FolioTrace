using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record EffectiveDateControlSettings(DateControlConfiguration Configuration, DateControlSettingsSource Source, EventID LastEventID, LastAuditDateTime LastAuditDateTime);

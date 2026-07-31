using FolioTrace.Types;
namespace FolioTrace.Aggregates;
public sealed record UserDateControlSettingsRequest(UserID UserID, EventDateTime EventDateTime, string Reason, DateControlConfiguration Configuration);

using FolioTrace.Types;
namespace FolioTrace.Aggregates;
public sealed record DateControlSettingsRequest(UserID UserID, EventDateTime EventDateTime, string Reason, DateControlConfiguration Configuration);

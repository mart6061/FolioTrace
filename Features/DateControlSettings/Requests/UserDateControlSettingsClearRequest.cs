using FolioTrace.Types;
namespace FolioTrace.Aggregates;
public sealed record UserDateControlSettingsClearRequest(UserID UserID, EventDateTime EventDateTime, string Reason);

using System.Text.Json.Serialization;
using FolioTrace.Types;

namespace FolioTrace.Common;

public abstract record AuditEventBase(EventID EventID, UserID UserID, AuditDateTime AuditDateTime) : IAuditEventBase
{
    [JsonIgnore]
    public Guid Id => EventID.Value;

    [JsonPropertyName("$type")]
    public abstract string Type { get; }
}

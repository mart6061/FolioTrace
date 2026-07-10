using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record InputPolicyResolveRequest(
    EventDateTime EventDateTime,
    AuditDateTime? AuditDateTime,
    IReadOnlyList<InputControlKind> ControlKinds,
    AccountID? AccountID,
    UserID? UserID,
    Alpha3? Currency,
    bool? AllowNegative);

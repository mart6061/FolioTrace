using System;

namespace AILibrary.Types;

public static class LastAuditDateTimeBuilder
{
    // Create a new LastAuditDateTime using current UTC time
    public static LastAuditDateTime Create() => new LastAuditDateTime(DateTime.UtcNow);

    // Create a LastAuditDateTime from the provided value
    public static LastAuditDateTime Create(DateTime value) => new LastAuditDateTime(value);

    // Create a LastAuditDateTime from an AuditDateTime
    public static LastAuditDateTime Create(AuditDateTime audit) => new LastAuditDateTime(audit.Value);
}

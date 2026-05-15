using System;

namespace AILibrary.Types;

public static class BuilderAuditDateTime
{
    // Create a new AuditDateTime using current UTC time
    public static AuditDateTime Create() => new AuditDateTime(DateTime.UtcNow);

    // Create an AuditDateTime from the provided value
    public static AuditDateTime Create(DateTime value) => new AuditDateTime(value);
}

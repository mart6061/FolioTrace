using System;

namespace AILibrary.Types;

public static class AuditDateTimeBuilder
{
    // Create a new AuditDateTime using current UTC time
    public static AuditDateTime Create() => new AuditDateTime(DateTime.UtcNow);

    // Create an AuditDateTime from the provided value
    public static AuditDateTime Create(DateTime value) => new AuditDateTime(value);

    // Create an AuditDateTime representing the beginning of time
    public static AuditDateTime CreateBeginningOfTime() => new AuditDateTime(DateTime.MinValue.AddTicks(1));

    // Create an AuditDateTime representing the end of time
    public static AuditDateTime CreateEndOfTime() => new AuditDateTime(DateTime.MaxValue);
}

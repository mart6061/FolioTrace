using System;

namespace AILibrary.Types;

public static class BuilderLastUpdatedDateTime
{
    // Create a new LastUpdatedDateTime using current UTC time
    public static LastUpdatedDateTime Create() => new LastUpdatedDateTime(DateTime.UtcNow);

    // Create a LastUpdatedDateTime from the provided value
    public static LastUpdatedDateTime Create(DateTime value) => new LastUpdatedDateTime(value);

    // Create a LastUpdatedDateTime from an AuditDateTime
    public static LastUpdatedDateTime Create(AuditDateTime audit) => new LastUpdatedDateTime(audit.Value);
}

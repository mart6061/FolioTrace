using System;

using FolioTrace.Common;

namespace FolioTrace.Types;

[Builder]
public static class EventDateTimeBuilder
{
    // Create a new EventDateTime using current UTC time
    public static EventDateTime Create() => new EventDateTime(DateTime.UtcNow);

    // Create an EventDateTime from the provided value
    public static EventDateTime Create(DateTime value) => new EventDateTime(value);

    // Create an EventDateTime representing the beginning of time
    //public static EventDateTime CreateBeginningOfTime() => new EventDateTime(DateTime.MinValue.AddTicks(1));

    // Create an EventDateTime representing the end of time
    //public static EventDateTime CreateEndOfTime() => new EventDateTime(DateTime.MaxValue);
}

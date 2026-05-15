using System;

namespace AILibrary.Types;

public static class BuilderEventDateTime
{
    // Create a new EventDateTime using current UTC time
    public static EventDateTime Create() => new EventDateTime(DateTime.UtcNow);

    // Create an EventDateTime from the provided value
    public static EventDateTime Create(DateTime value) => new EventDateTime(value);
}

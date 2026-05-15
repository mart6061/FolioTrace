using System;

namespace AILibrary.Types;

public static class BuilderISO3
{
    // Create a new ISO3 from provided value (validation enforced by ISO3 constructor)
    public static ISO3 Create(string value) => new ISO3(value);
}

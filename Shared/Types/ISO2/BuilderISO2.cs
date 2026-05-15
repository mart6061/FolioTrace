using System;

namespace AILibrary.Types;

public static class BuilderISO2
{
    // Create a new ISO2 from provided value (validation enforced by ISO2 constructor)
    public static ISO2 Create(string value) => new ISO2(value);
}

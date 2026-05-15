using System;

namespace AILibrary.Types;

public static class BuilderISIN
{
    // Create a new ISIN from provided value (validation enforced by ISIN constructor)
    public static ISIN Create(string value) => new ISIN(value);
}

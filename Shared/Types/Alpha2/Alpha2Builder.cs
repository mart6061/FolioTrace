using System;

namespace FolioTrace.Types;

public static class Alpha2Builder
{
    // Create a new Alpha2 from provided value (validation enforced by Alpha2 constructor)
    public static Alpha2 Create(string value) => new Alpha2(value);
}

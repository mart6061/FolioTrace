using System;

using FolioTrace.Common;

namespace FolioTrace.Types;

[Builder]
public static class Alpha3Builder
{
    // Create a new Alpha3 from provided value (validation enforced by Alpha3 constructor)
    public static Alpha3 Create(string value) => new Alpha3(value);
}

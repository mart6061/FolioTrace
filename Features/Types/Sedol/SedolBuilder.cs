using System;

using FolioTrace.Common;

namespace FolioTrace.Types;

[Builder]
public static class SedolBuilder
{
    // Create a new Sedol from provided value (validation enforced by Sedol constructor)
    public static Sedol Create(string value) => new Sedol(value);
}

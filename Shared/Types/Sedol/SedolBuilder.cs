using System;

namespace AILibrary.Types;

public static class SedolBuilder
{
    // Create a new Sedol from provided value (validation enforced by Sedol constructor)
    public static Sedol Create(string value) => new Sedol(value);
}

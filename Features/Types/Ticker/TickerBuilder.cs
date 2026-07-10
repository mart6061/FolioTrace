using System;

using FolioTrace.Common;

namespace FolioTrace.Types;

[Builder]
public static class TickerBuilder
{
    // Create a new Ticker from provided value (validation enforced by Ticker constructor)
    public static Ticker Create(string value) => new Ticker(value);
}

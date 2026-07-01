using FolioTrace.Common;

namespace FolioTrace.Types;

[Builder]
public static class InstrumentDateBuilder
{
    public static InstrumentDate Create() => new InstrumentDate(DateOnly.FromDateTime(DateTime.UtcNow));

    public static InstrumentDate Create(DateOnly? value) => new InstrumentDate(value);

    public static InstrumentDate Create(DateTime value) => new InstrumentDate(DateOnly.FromDateTime(value));
}

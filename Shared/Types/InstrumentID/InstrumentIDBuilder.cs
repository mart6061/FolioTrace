using System;

namespace FolioTrace.Types;

public static class InstrumentIDBuilder
{
    // Create a new InstrumentID with a newly generated non-empty GUID
    public static InstrumentID Create() => new InstrumentID(Guid.NewGuid());

    // Restore an InstrumentID from an existing GUID value
    public static InstrumentID Restore(Guid value) => new InstrumentID(value);
}

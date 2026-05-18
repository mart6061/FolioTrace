using FolioTrace.Types;

namespace FolioTrace.Common;

public interface IEventRequest
{
    UserID UserID { get; }

    EventDateTime EventDateTime { get; }

    string Reason { get; }
}

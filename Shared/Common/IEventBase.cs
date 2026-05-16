using FolioTrace.Types;

namespace FolioTrace.Common;

// Event base interface placed in Events folder but uses the top-level FolioTrace.Types namespace
public interface IEventBase : IType
{
    string Type { get; }

    EventID EventID { get; }

    EventDateTime EventDateTime { get; }

    AuditDateTime AuditDateTime { get; }

    string Reason { get; }
}

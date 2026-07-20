namespace FolioTrace.Common;

/// <summary>
/// The canonical (EventDateTime, AuditDateTime, EventID) ordering that event streams are stored in and that
/// aggregate constructors build from. Extracted from InMemoryEventsRepository so it can also be used to
/// determine "events after a given boundary" independently of any particular repository implementation.
/// </summary>
public static class EventOrderComparer
{
    public static int Compare(IAuditEventBase left, IAuditEventBase right)
    {
        if (left is IEventBase leftTimed && right is IEventBase rightTimed)
        {
            var eventDateComparison = leftTimed.EventDateTime.Value.CompareTo(rightTimed.EventDateTime.Value);
            if (eventDateComparison != 0)
                return eventDateComparison;
        }

        var auditDateComparison = left.AuditDateTime.Value.CompareTo(right.AuditDateTime.Value);
        if (auditDateComparison != 0)
            return auditDateComparison;

        return left.EventID.Value.CompareTo(right.EventID.Value);
    }
}

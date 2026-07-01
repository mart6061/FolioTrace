using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

[Builder]
public static class AccountsBuilder
{
    public static Accounts Create(EventDateTime eventDate, AuditDateTime auditDateTime, List<IAccountEvent> accountEvents)
    {
        if (eventDate is null)
            throw new ArgumentNullException(nameof(eventDate));
        if (auditDateTime is null)
            throw new ArgumentNullException(nameof(auditDateTime));
        if (accountEvents is null)
            throw new ArgumentNullException(nameof(accountEvents));

        return new Accounts(eventDate.Value, auditDateTime, accountEvents);
    }
}

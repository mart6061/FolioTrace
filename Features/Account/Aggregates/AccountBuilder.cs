using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public static class AccountBuilder
{
    public static Account Create(AccountCreatedEvent createdEvent, DisplayOrder displayOrder)
    {
        if (createdEvent is null)
            throw new ArgumentNullException(nameof(createdEvent));

        return new Account(createdEvent.AccountID, createdEvent.Name, createdEvent.FormalName, createdEvent.BookCurrency, createdEvent.Active, displayOrder, createdEvent.EventDateTime, createdEvent.AuditDateTime, createdEvent.EventID);
    }

    extension(Account account)
    {
        public Account Apply(AccountModifiedEvent modifiedEvent)
        {
            if (account is null)
                throw new ArgumentNullException(nameof(account));

            if (modifiedEvent is null)
                throw new ArgumentNullException(nameof(modifiedEvent));

            return account with
            {
                Name = modifiedEvent.Name,
                FormalName = modifiedEvent.FormalName,
                ValuationDateTime = modifiedEvent.EventDateTime,
                AsOfDateTime = modifiedEvent.AuditDateTime,
                LastEventID = modifiedEvent.EventID,
                LastAuditDateTime = modifiedEvent.AuditDateTime
            };
        }

        public Account Apply(AccountActiveSetEvent activeModifiedEvent)
        {
            if (account is null)
                throw new ArgumentNullException(nameof(account));

            if (activeModifiedEvent is null)
                throw new ArgumentNullException(nameof(activeModifiedEvent));

            return account with
            {
                Active = activeModifiedEvent.Active,
                ValuationDateTime = activeModifiedEvent.EventDateTime,
                AsOfDateTime = activeModifiedEvent.AuditDateTime,
                LastEventID = activeModifiedEvent.EventID,
                LastAuditDateTime = activeModifiedEvent.AuditDateTime
            };
        }

        public Account Apply(AccountDisplayOrderSetEvent displayOrderSetEvent)
        {
            if (account is null)
                throw new ArgumentNullException(nameof(account));

            if (displayOrderSetEvent is null)
                throw new ArgumentNullException(nameof(displayOrderSetEvent));

            return account with
            {
                DisplayOrder = displayOrderSetEvent.DisplayOrder,
                ValuationDateTime = displayOrderSetEvent.EventDateTime,
                AsOfDateTime = displayOrderSetEvent.AuditDateTime,
                LastEventID = displayOrderSetEvent.EventID,
                LastAuditDateTime = displayOrderSetEvent.AuditDateTime
            };
        }
    }
}

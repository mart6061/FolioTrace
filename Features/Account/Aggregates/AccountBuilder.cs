using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

[Builder]
public static class AccountBuilder
{
    public static Account Create(AccountCreatedEvent createdEvent, DisplayOrder displayOrder)
    {
        if (createdEvent is null)
            throw new ArgumentNullException(nameof(createdEvent));

        return new Account(createdEvent.AccountID, createdEvent.Name, createdEvent.FormalName, createdEvent.BookCurrency, createdEvent.BookCostBasis, [], createdEvent.Active, displayOrder, createdEvent.EventDateTime, createdEvent.AuditDateTime, createdEvent.EventID);
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
                BookCostBasis = modifiedEvent.BookCostBasis,
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

        public Account Apply(AccountIdentifierSetEvent setEvent)
        {
            var identifiers = account.Identifiers
                .Where(identifier => identifier.Type != setEvent.Identifier.Type)
                .Append(setEvent.Identifier)
                .OrderBy(identifier => identifier.Type)
                .ToList();

            return account with
            {
                Identifiers = identifiers,
                ValuationDateTime = setEvent.EventDateTime,
                AsOfDateTime = setEvent.AuditDateTime,
                LastEventID = setEvent.EventID,
                LastAuditDateTime = setEvent.AuditDateTime
            };
        }

        public Account Apply(AccountIdentifierUnsetEvent unsetEvent)
        {
            return account with
            {
                Identifiers = account.Identifiers.Where(identifier => identifier.Type != unsetEvent.IdentifierType).ToList(),
                ValuationDateTime = unsetEvent.EventDateTime,
                AsOfDateTime = unsetEvent.AuditDateTime,
                LastEventID = unsetEvent.EventID,
                LastAuditDateTime = unsetEvent.AuditDateTime
            };
        }
    }
}

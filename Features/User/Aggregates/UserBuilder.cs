using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public static class UserBuilder
{
    public static User Create(UserID userId, string displayName, UserDisplayPreferences displayPreferences, UserProfileValuationPreferences valuationPreferences)
    {
        var createdEvent = UserCreatedEventBuilder.Create(
            userId,
            EventDateTimeBuilder.Create(),
            $"Create {nameof(User)}",
            displayName,
            displayPreferences,
            valuationPreferences);

        return Create(createdEvent.Value!);
    }

    public static User CreateSeed(string displayName, UserDisplayPreferences displayPreferences, UserProfileValuationPreferences valuationPreferences)
    {
        var createdEvent = UserCreatedEventBuilder.CreateSeed(
            Guid.CreateGuid7(),
            Constants.Initialisation.UserID,
            Constants.Initialisation.EventDateTime,
            Constants.Initialisation.AuditDateTime,
            Constants.Initialisation.Reason,
            displayName,
            displayPreferences,
            valuationPreferences);

        return Create(createdEvent.Value!);
    }

    // Create a new User from a UserCreatedEvent
    public static User Create(UserCreatedEvent createdEvent)
    {
        if (createdEvent is null)
            throw new ArgumentNullException(nameof(createdEvent));

        return new User(
            createdEvent.UserID,
            createdEvent.DisplayName,
            createdEvent.DisplayPreferences,
            createdEvent.ValuationPreferences,
            null,
            null,
            createdEvent.EventDateTime,
            createdEvent.AuditDateTime,
            createdEvent.EventID,
            createdEvent.AuditDateTime);
    }

    extension(User user)
    {
        // Apply a UserModifiedEvent to an existing User and use the audit timestamp as the last audit time
        public User Apply(UserModifiedEvent modifiedEvent)
        {
            if (user is null)
                throw new ArgumentNullException(nameof(user));

            if (modifiedEvent is null)
                throw new ArgumentNullException(nameof(modifiedEvent));

            return user with
            {
                DisplayName = modifiedEvent.DisplayName,
                DisplayPreferences = modifiedEvent.DisplayPreferences,
                ValuationPreferences = modifiedEvent.ValuationPreferences,
                ValuationDateTime = modifiedEvent.EventDateTime,
                AsOfDateTime = modifiedEvent.AuditDateTime,
                LastEventID = modifiedEvent.EventID,
                LastAuditDateTime = modifiedEvent.AuditDateTime
            };
        }

        public User Apply(UserSignedInEvent signedInEvent)
        {
            if (user is null)
                throw new ArgumentNullException(nameof(user));

            if (signedInEvent is null)
                throw new ArgumentNullException(nameof(signedInEvent));

            return user with
            {
                LastSignedIn = signedInEvent.EventDateTime,
                ValuationDateTime = signedInEvent.EventDateTime,
                AsOfDateTime = signedInEvent.AuditDateTime,
                LastEventID = signedInEvent.EventID,
                LastAuditDateTime = signedInEvent.AuditDateTime
            };
        }

        public User Apply(UserSignedOutEvent signedOutEvent)
        {
            if (user is null)
                throw new ArgumentNullException(nameof(user));

            if (signedOutEvent is null)
                throw new ArgumentNullException(nameof(signedOutEvent));

            return user with
            {
                LastSignedOut = signedOutEvent.EventDateTime,
                ValuationDateTime = signedOutEvent.EventDateTime,
                AsOfDateTime = signedOutEvent.AuditDateTime,
                LastEventID = signedOutEvent.EventID,
                LastAuditDateTime = signedOutEvent.AuditDateTime
            };
        }
    }
}

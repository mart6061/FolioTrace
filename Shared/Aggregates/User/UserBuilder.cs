using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public static class UserBuilder
{
    public static User Create(UserID userId, string displayName, UserDisplayPreferences displayPreferences, UserValuationPreferences valuationPreferences)
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

    public static User CreateSeed(string displayName, UserDisplayPreferences displayPreferences, UserValuationPreferences valuationPreferences)
    {
        var createdEvent = UserCreatedEventBuilder.CreateSeed(
            Guid.NewGuid(),
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
    }
}

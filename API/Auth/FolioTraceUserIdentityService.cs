using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using FolioTrace;
using FolioTrace.Aggregates;
using FolioTrace.Common;
using FolioTrace.Types;
using Repository;
using Services;

namespace API.Auth;

public sealed class FolioTraceUserIdentityService
{
    private const string UserIDNamespace = "FolioTrace.WorkOS.EmailUserID.v1:";
    private readonly IEventRepository eventRepository;
    private readonly UserService userService;
    private readonly Action<IAuditEventBase> invalidate;
    private readonly ConcurrentDictionary<Guid, SemaphoreSlim> userCreationLocks = new();

    public FolioTraceUserIdentityService(
        IEventRepository eventRepository,
        AggregateCacheInvalidationService cacheInvalidationService,
        UserService userService)
        : this(eventRepository, userService, @event => cacheInvalidationService.Invalidate(@event))
    {
    }

    internal FolioTraceUserIdentityService(
        IEventRepository eventRepository,
        UserService userService,
        Action<IAuditEventBase> invalidate)
    {
        this.eventRepository = eventRepository;
        this.userService = userService;
        this.invalidate = invalidate;
    }

    public FolioTraceUserIdentity CreateIdentity(WorkOSProfile profile)
    {
        var email = NormalizeEmail(profile.Email);
        if (string.IsNullOrWhiteSpace(email))
            throw new InvalidOperationException("Authenticated WorkOS profile does not have an email address.");

        var workOSUserID = profile.Id?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(workOSUserID))
            throw new InvalidOperationException("Authenticated WorkOS profile does not have an id.");

        var organizationID = profile.OrganizationId?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(organizationID))
            throw new InvalidOperationException("Authenticated WorkOS profile does not have an organization id.");

        var displayName = string.Join(' ', new[] { profile.FirstName, profile.LastName }
                .Where(part => !string.IsNullOrWhiteSpace(part))
                .Select(part => part!.Trim()))
            .Trim();

        if (string.IsNullOrWhiteSpace(displayName))
            displayName = email;

        return new FolioTraceUserIdentity(
            DeriveUserIDFromEmail(email),
            workOSUserID,
            email,
            displayName,
            organizationID);
    }

    public async Task EnsureUserAsync(FolioTraceUserIdentity identity, CancellationToken cancellationToken)
    {
        var userID = new UserID(identity.UserID);
        if (await userService.FindCurrentAsync(userID, cancellationToken) is not null)
            return;

        var creationLock = userCreationLocks.GetOrAdd(identity.UserID, static _ => new SemaphoreSlim(1, 1));
        await creationLock.WaitAsync(cancellationToken);
        try
        {
            if (await userService.FindCurrentAsync(userID, cancellationToken) is not null)
                return;

            var eventDateTime = EventDateTimeBuilder.Create(DateTime.UtcNow);
            var userResult = UserCreatedEventBuilder.Create(
                userID,
                eventDateTime,
                "Create WorkOS user",
                identity.DisplayName,
                new UserDisplayPreferences(false, false),
                new UserProfileValuationPreferences(eventDateTime, true, true));

            if (!userResult.IsValid || userResult.Value is null)
                throw new InvalidOperationException($"Unable to create user: {string.Join("; ", userResult.ValidationErrors)}");

            var menuPreferencesResult = UserMenuPreferencesCreatedEventBuilder.CreateDefault(userResult.Value);
            var valuationPreferencesResult = UserValuationPreferencesCreatedEventBuilder.CreateDefault(userResult.Value);

            if (!menuPreferencesResult.IsValid || menuPreferencesResult.Value is null)
                throw new InvalidOperationException($"Unable to create default menu preferences: {string.Join("; ", menuPreferencesResult.ValidationErrors)}");

            if (!valuationPreferencesResult.IsValid || valuationPreferencesResult.Value is null)
                throw new InvalidOperationException($"Unable to create default valuation preferences: {string.Join("; ", valuationPreferencesResult.ValidationErrors)}");

            await eventRepository.AppendAsync(Constants.Initialisation.UsersStreamId, userResult.Value, cancellationToken);
            await eventRepository.AppendAsync(Constants.Initialisation.UserMenuPreferencesStreamId, menuPreferencesResult.Value, cancellationToken);
            await eventRepository.AppendAsync(Constants.Initialisation.UserValuationPreferencesStreamId, valuationPreferencesResult.Value, cancellationToken);
            invalidate(userResult.Value);
            invalidate(menuPreferencesResult.Value);
            invalidate(valuationPreferencesResult.Value);
        }
        finally
        {
            creationLock.Release();
        }
    }

    public async Task RecordSignInAsync(FolioTraceUserIdentity identity, CancellationToken cancellationToken)
    {
        var result = UserSignedInEventBuilder.Create(
            new UserID(identity.UserID),
            EventDateTimeBuilder.Create(DateTime.UtcNow),
            "WorkOS user signed in");

        if (!result.IsValid || result.Value is null)
            throw new InvalidOperationException($"Unable to record sign in: {string.Join("; ", result.ValidationErrors)}");

        await eventRepository.AppendAsync(Constants.Initialisation.UsersStreamId, result.Value, cancellationToken);
        invalidate(result.Value);
    }

    public async Task RecordSignOutAsync(Guid userID, CancellationToken cancellationToken)
    {
        var result = UserSignedOutEventBuilder.Create(
            new UserID(userID),
            EventDateTimeBuilder.Create(DateTime.UtcNow),
            "WorkOS user signed out");

        if (!result.IsValid || result.Value is null)
            throw new InvalidOperationException($"Unable to record sign out: {string.Join("; ", result.ValidationErrors)}");

        await eventRepository.AppendAsync(Constants.Initialisation.UsersStreamId, result.Value, cancellationToken);
        invalidate(result.Value);
    }

    public static Guid DeriveUserIDFromEmail(string email)
    {
        var normalizedEmail = NormalizeEmail(email);
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes($"{UserIDNamespace}{normalizedEmail}"))[..16];
        bytes[6] = (byte)((bytes[6] & 0x0f) | 0x50);
        bytes[8] = (byte)((bytes[8] & 0x3f) | 0x80);
        return new Guid(bytes, bigEndian: true);
    }

    private static string NormalizeEmail(string? email) => email?.Trim().ToLowerInvariant() ?? string.Empty;
}

using FolioTrace;
using FolioTrace.Aggregates;
using FolioTrace.Types;
using Repository;
using Services;

namespace API;

public static partial class ApiEndpointRegistration
{
    private static void MapDateControlSettingsEndpoints(this RouteGroupBuilder api)
    {
        var settings = api.MapGroup("/DateControlSettings").WithTags("Date Control Settings");

        settings.MapGet("/", async (DateTime eventDateTime, DateTime? auditDateTime, DateControlSettingsService service) =>
        {
            var valuationDate = EventDateTimeBuilder.Create(eventDateTime);
            return Results.Ok(await GetAsAt(auditDateTime, () => service.Get(valuationDate), asAt => service.Get(valuationDate, asAt)));
        });

        settings.MapGet("/User", async (Guid userID, DateTime eventDateTime, DateTime? auditDateTime, UserDateControlSettingsService service) =>
        {
            var valuationDate = EventDateTimeBuilder.Create(eventDateTime);
            var resolvedUserID = new UserID(userID);
            return Results.Ok(await GetAsAt(auditDateTime, () => service.Get(resolvedUserID, valuationDate), asAt => service.Get(resolvedUserID, valuationDate, asAt)));
        });

        settings.MapGet("/Effective", async (Guid userID, DateTime eventDateTime, DateTime? auditDateTime, EffectiveDateControlSettingsService service) =>
        {
            var valuationDate = EventDateTimeBuilder.Create(eventDateTime);
            var asAt = auditDateTime.HasValue ? AuditDateTimeBuilder.Create(auditDateTime.Value) : null;
            return Results.Ok(await service.Get(new UserID(userID), valuationDate, asAt));
        });
    }

    private static void MapDateControlSettingsEventEndpoints(this RouteGroupBuilder api)
    {
        var global = api.MapGroup("/Events/DateControlSettings").WithTags("Date Control Settings Events");
        global.MapGet("/", async (DateTime? valuationDateTime, DateTime? auditDateTime, IEventRepository repository, CancellationToken cancellationToken) =>
            Results.Ok(EventHistoryResponseFactory.Create(await repository.LoadStreamAsync<IDateControlSettingsEvent>(Constants.Initialisation.DateControlSettingsStreamId, cancellationToken), valuationDateTime, auditDateTime, item => item)));
        global.MapPost($"/{nameof(DateControlSettingsCreatedEvent)}", async (IEventRepository repository, AggregateCacheInvalidationService invalidation, DateControlSettingsRequest request, CancellationToken cancellationToken) =>
            await EventEndpointFactory.CreateAndAppend(Constants.Initialisation.DateControlSettingsStreamId, DateControlSettingsEventsRoute, repository, invalidation, () => DateControlSettingsCreatedEventBuilder.Create(request), cancellationToken));
        global.MapPost($"/{nameof(DateControlSettingsModifiedEvent)}", async (IEventRepository repository, AggregateCacheInvalidationService invalidation, DateControlSettingsRequest request, CancellationToken cancellationToken) =>
            await EventEndpointFactory.CreateAndAppend(Constants.Initialisation.DateControlSettingsStreamId, DateControlSettingsEventsRoute, repository, invalidation, () => DateControlSettingsModifiedEventBuilder.Create(request), cancellationToken));

        var user = api.MapGroup("/Events/UserDateControlSettings").WithTags("User Date Control Settings Events");
        user.MapGet("/", async (Guid? userID, DateTime? valuationDateTime, DateTime? auditDateTime, IEventRepository repository, CancellationToken cancellationToken) =>
        {
            var events = await repository.LoadStreamAsync<IUserDateControlSettingsEvent>(Constants.Initialisation.UserDateControlSettingsStreamId, cancellationToken);
            if (userID.HasValue) events = events.Where(item => item.UserID.Value == userID.Value).ToList();
            return Results.Ok(EventHistoryResponseFactory.Create(events, valuationDateTime, auditDateTime, item => item));
        });
        user.MapPost($"/{nameof(UserDateControlSettingsCreatedEvent)}", async (IEventRepository repository, AggregateCacheInvalidationService invalidation, UserDateControlSettingsRequest request, CancellationToken cancellationToken) =>
            await EventEndpointFactory.CreateAndAppend(Constants.Initialisation.UserDateControlSettingsStreamId, UserDateControlSettingsEventsRoute, repository, invalidation, () => UserDateControlSettingsCreatedEventBuilder.Create(request), cancellationToken));
        user.MapPost($"/{nameof(UserDateControlSettingsModifiedEvent)}", async (IEventRepository repository, AggregateCacheInvalidationService invalidation, UserDateControlSettingsRequest request, CancellationToken cancellationToken) =>
            await EventEndpointFactory.CreateAndAppend(Constants.Initialisation.UserDateControlSettingsStreamId, UserDateControlSettingsEventsRoute, repository, invalidation, () => UserDateControlSettingsModifiedEventBuilder.Create(request), cancellationToken));
        user.MapPost($"/{nameof(UserDateControlSettingsClearedEvent)}", async (IEventRepository repository, AggregateCacheInvalidationService invalidation, UserDateControlSettingsClearRequest request, CancellationToken cancellationToken) =>
            await EventEndpointFactory.CreateAndAppend(Constants.Initialisation.UserDateControlSettingsStreamId, UserDateControlSettingsEventsRoute, repository, invalidation, () => UserDateControlSettingsClearedEventBuilder.Create(request), cancellationToken));
    }
}

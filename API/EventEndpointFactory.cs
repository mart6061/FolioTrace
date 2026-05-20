using FolioTrace.Common;
using Repository;
using Services;

namespace API;

public static class EventEndpointFactory
{
    public static async Task<IResult> CreateAndAppend<TEvent>(
        Guid streamId,
        string eventRoute,
        IEventRepository eventRepository,
        AggregateCacheInvalidationService cacheInvalidationService,
        Func<Result<TEvent>> createEvent,
        CancellationToken cancellationToken)
        where TEvent : class, IEventBase
    {
        var result = Create(createEvent);
        if (!result.IsValid || result.Value is null)
            return Results.BadRequest(result);

        await eventRepository.AppendAsync(streamId, result.Value, cancellationToken);
        cacheInvalidationService.Invalidate(result.Value);

        return Results.Accepted(eventRoute, CreateAcceptedEventResponse(eventRoute, result.Value));
    }

    public static object CreateAcceptedEventResponse(string eventRoute, IEventBase @event) =>
        new
        {
            EventID = @event.EventID.Value,
            Links = new[]
            {
                new
                {
                    Rel = "self",
                    Href = $"{eventRoute}/{@event.EventID.Value}",
                    Method = "GET"
                }
            }
        };

    private static Result<TEvent> Create<TEvent>(Func<Result<TEvent>> createEvent) =>
        TryCreate(createEvent);

    private static Result<TEvent> TryCreate<TEvent>(Func<Result<TEvent>> createEvent)
    {
        try
        {
            return createEvent();
        }
        catch (ArgumentException exception)
        {
            return Result<TEvent>.Invalid([exception.Message]);
        }
        catch (FormatException exception)
        {
            return Result<TEvent>.Invalid([exception.Message]);
        }
        catch (NullReferenceException exception)
        {
            return Result<TEvent>.Invalid([exception.Message]);
        }
    }
}

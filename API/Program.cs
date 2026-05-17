using FolioTrace;
using FolioTrace.Aggregates;
using FolioTrace.Common;
using FolioTrace.Types;
using Repository;
using Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddFolioTraceRepository(builder.Configuration);
builder.Services.AddFolioTraceServices();

var app = builder.Build();

app.UsePathBase("/API");

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

var api = app.MapGroup("");
var countries = api.MapGroup("/Countries");
var countryEvents = api.MapGroup("/Events/Country");

countries.MapGet("/", async (DateTime eventDateTime, DateTime? auditDateTime, CountryService countryService) =>
{
    var valuationDate = EventDateTimeBuilder.Create(eventDateTime);

    return auditDateTime.HasValue
        ? Results.Ok(await countryService.Get(valuationDate, AuditDateTimeBuilder.Create(auditDateTime.Value)))
        : Results.Ok(await countryService.Get(valuationDate));
});

countryEvents.MapGet("/", async (IEventRepository eventRepository, CancellationToken cancellationToken) =>
    await eventRepository.LoadStreamAsync<ICountryEvent>(Constants.Initialisation.CountriesStreamId, cancellationToken));

countryEvents.MapGet("/{eventId:guid}", async (Guid eventId, IEventRepository eventRepository, CancellationToken cancellationToken) =>
{
    var @event = await eventRepository.LoadAsync<IEventBase>(eventId, cancellationToken);
    return @event is ICountryEvent
        ? Results.Ok(@event)
        : Results.NotFound();
});

countryEvents.MapPost("/", async (IEventRepository eventRepository, IEnumerable<IEventBase> events, CancellationToken cancellationToken) =>
{
    var eventData = events.ToList();
    if (eventData.Any(@event => @event is not ICountryEvent))
        return Results.BadRequest("All events must be country events.");

    await eventRepository.AppendAsync(Constants.Initialisation.CountriesStreamId, eventData, cancellationToken);

    return Results.Accepted(
        "/API/Events/Country",
        eventData.Select(@event => new
        {
            EventID = @event.EventID.Value,
            Links = new[]
            {
                new
                {
                    Rel = "self",
                    Href = $"/API/Events/Country/{@event.EventID.Value}",
                    Method = "GET"
                }
            }
        }));
});

app.Run();

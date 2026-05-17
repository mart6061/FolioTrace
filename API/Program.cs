using FolioTrace;
using FolioTrace.Aggregates;
using FolioTrace.Common;
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
var countryEvents = api.MapGroup("/Events/Country");

countryEvents.MapGet("/", async (IEventRepository eventRepository, CancellationToken cancellationToken) =>
    await eventRepository.LoadStreamAsync<ICountryEvent>(Constants.Initialisation.CountriesStreamId, cancellationToken));

countryEvents.MapPost("/", async (IEventRepository eventRepository, IEnumerable<IEventBase> events, CancellationToken cancellationToken) =>
{
    var eventData = events.ToList();
    if (eventData.Any(@event => @event is not ICountryEvent))
        return Results.BadRequest("All events must be country events.");

    await eventRepository.AppendAsync(Constants.Initialisation.CountriesStreamId, eventData, cancellationToken);
    return Results.Accepted($"/API/Events/Country", new { Count = eventData.Count });
});

app.Run();

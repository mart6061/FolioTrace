using FolioTrace;
using FolioTrace.Aggregates;
using FolioTrace.Types;
using Repository;

namespace Services;

public sealed class CountryService(IEventRepository eventRepository)
{
    public async Task<Countries> Get(EventDateTime valuationDate)
    {
        if (valuationDate is null)
            throw new ArgumentNullException(nameof(valuationDate));

        var events = await eventRepository.LoadStreamAsync<ICountryEvent>(Constants.Initialisation.CountriesStreamId);
        return new Countries(valuationDate, events.ToList());
    }

    public async Task<Countries> Get(EventDateTime valuationDate, AuditDateTime asAt)
    {
        if (valuationDate is null)
            throw new ArgumentNullException(nameof(valuationDate));

        if (asAt is null)
            throw new ArgumentNullException(nameof(asAt));

        var events = await eventRepository.LoadStreamAsync<ICountryEvent>(Constants.Initialisation.CountriesStreamId);
        return new Countries(valuationDate, asAt, events.ToList());
    }
}

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using FolioTrace;
using Repository;
using Services;

var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
    .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: false)
    .AddEnvironmentVariables()
    .Build();

using var services = new ServiceCollection()
    .AddSingleton<IConfiguration>(configuration)
    .AddFolioTraceRepository(configuration)
    .AddFolioTraceServices()
    .BuildServiceProvider();

while (true)
{
    Console.WriteLine("FolioTrace");
    Console.WriteLine("1. Run initialisation");
    Console.WriteLine("2. Reference Data");
    Console.WriteLine("C. Close");
    Console.Write("Select an option: ");

    var option = Console.ReadLine()?.Trim();
    Console.WriteLine();

    switch (option?.ToUpperInvariant())
    {
        case "1":
            await RunInitialisation(services);
            break;
        case "2":
            await ShowReferenceDataMenu(services);
            break;
        case "C":
            return;
        default:
            Console.WriteLine("Invalid option.");
            break;
    }

    Console.WriteLine();
}

static async Task RunInitialisation(ServiceProvider services)
{
    try
    {
        using var scope = services.CreateScope();
        var initRepository = scope.ServiceProvider.GetRequiredService<IInitRepository>();

        Console.WriteLine("Running initialisation...");
        await initRepository.Build();
        Console.WriteLine("Initialisation complete.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Initialisation failed: {ex.Message}");
    }
}

static async Task ShowReferenceDataMenu(ServiceProvider services)
{
    while (true)
    {
        Console.WriteLine("Reference Data");
        Console.WriteLine("1. Display Countries");
        Console.WriteLine("R. Return");
        Console.Write("Select an option: ");

        var option = Console.ReadLine()?.Trim();
        Console.WriteLine();

        switch (option?.ToUpperInvariant())
        {
            case "1":
                await DisplayCountries(services);
                break;
            case "R":
                return;
            default:
                Console.WriteLine("Invalid option.");
                break;
        }

        Console.WriteLine();
    }
}

static async Task DisplayCountries(ServiceProvider services)
{
    try
    {
        using var scope = services.CreateScope();
        var countryService = scope.ServiceProvider.GetRequiredService<CountryService>();

        var countries = await countryService.Get(Constants.Valuation.Today);

        Console.WriteLine($"Countries as at {countries.ValuationDateTime}");
        Console.WriteLine($"{"Alpha2",-6} {"Alpha3",-6} {"Numeric",7}");

        foreach (var country in countries.Items.OrderBy(country => country.Alpha2.Value))
            Console.WriteLine($"{country.Alpha2.Value,-6} {country.Alpha3.Value,-6} {country.Numeric,7:D3}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Display countries failed: {ex.Message}");
    }
}

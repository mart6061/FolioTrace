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
    Console.WriteLine("1. Run seed");
    Console.WriteLine("2. Reference Data");
    Console.WriteLine("C. Close");
    Console.Write("Select an option: ");

    var option = Console.ReadLine()?.Trim();
    Console.WriteLine();

    switch (option?.ToUpperInvariant())
    {
        case "1":
            await RunSeed(services);
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

static async Task RunSeed(ServiceProvider services)
{
    try
    {
        using var scope = services.CreateScope();
        var seedRepository = scope.ServiceProvider.GetRequiredService<ISeedRepository>();

        Console.WriteLine("Running seed...");
        await seedRepository.Build();
        Console.WriteLine("Seed complete.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Seed failed: {ex.Message}");
    }
}

static async Task ShowReferenceDataMenu(ServiceProvider services)
{
    while (true)
    {
        Console.WriteLine("Reference Data");
        Console.WriteLine("1. Display Countries");
        Console.WriteLine("2. Display Currencies");
        Console.WriteLine("R. Return");
        Console.Write("Select an option: ");

        var option = Console.ReadLine()?.Trim();
        Console.WriteLine();

        switch (option?.ToUpperInvariant())
        {
            case "1":
                await DisplayCountries(services);
                break;
            case "2":
                await DisplayCurrencies(services);
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

static async Task DisplayCurrencies(ServiceProvider services)
{
    try
    {
        using var scope = services.CreateScope();
        var currencyService = scope.ServiceProvider.GetRequiredService<CurrencyService>();

        var currencies = await currencyService.Get(Constants.Valuation.Today);

        Console.WriteLine($"Currencies as at {currencies.ValuationDateTime}");
        Console.WriteLine($"{"Code",-6} {"Numeric",7} {"Decimals",8} Name");

        foreach (var currency in currencies.Items.OrderBy(currency => currency.AlphabeticCode.Value))
            Console.WriteLine($"{currency.AlphabeticCode.Value,-6} {currency.NumericCode,7:D3} {currency.DecimalPlace,8} {currency.Name}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Display currencies failed: {ex.Message}");
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
        Console.WriteLine($"{"Alpha2",-6} {"Alpha3",-6} {"Numeric",7} Name");

        foreach (var country in countries.Items.OrderBy(country => country.Alpha2.Value))
            Console.WriteLine($"{country.Alpha2.Value,-6} {country.Alpha3.Value,-6} {country.Numeric,7:D3} {country.Name}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Display countries failed: {ex.Message}");
    }
}


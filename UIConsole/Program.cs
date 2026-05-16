using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Repository;

var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
    .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: false)
    .AddEnvironmentVariables()
    .Build();

while (true)
{
    Console.WriteLine("FolioTrace");
    Console.WriteLine("1. Run initialisation");
    Console.WriteLine("2. Close");
    Console.Write("Select an option: ");

    var option = Console.ReadLine();
    Console.WriteLine();

    switch (option)
    {
        case "1":
            await RunInitialisation(configuration);
            break;
        case "2":
            return;
        default:
            Console.WriteLine("Invalid option.");
            break;
    }

    Console.WriteLine();
}

static async Task RunInitialisation(IConfiguration configuration)
{
    try
    {
        using var services = new ServiceCollection()
            .AddSingleton<IConfiguration>(configuration)
            .AddFolioTraceRepository(configuration)
            .BuildServiceProvider();

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

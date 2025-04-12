using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using testing.ApiClient;
using testing.Services;
using Cache;

class Program
{
    static async Task Main(string[] args)
    {
        // Create and configure the host
        using var host = Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                // Register services
                services.AddSingleton<ApiClient>(provider =>
                    new ApiClient("http://localhost:5000/", "object Object UA"));
                services.AddSingleton<LocationsCache>();
                services.AddSingleton<BlacklistCache>();
                services.AddSingleton<DispatchService>();
            })
            .ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddConsole();
                logging.SetMinimumLevel(LogLevel.Information);
            })
            .Build();

        var cts = new CancellationTokenSource();

        var count = 0;
        Console.CancelKeyPress += (sender, e) =>
        {
            count++;
            Console.WriteLine("Shutting down...");
            cts.Cancel();
            e.Cancel = true;
            if (count > 1)
            {
                Console.WriteLine("Forcefully shutting down...");
                Environment.Exit(0);
            }
        };

        var dispatcher = host.Services.GetRequiredService<DispatchService>();

        try
        {
            await dispatcher.RunDispatcher(
                targetDispatches: 10_000,
                maxActiveCalls: 10,
                cancellationToken: cts.Token);
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("Operation was canceled");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }

        Console.WriteLine("Press any key to exit...");
        Console.ReadKey();
    }
}
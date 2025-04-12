using Cache;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using testing.ApiClient;
using testing.Services;

class Program
{
    static async Task Main(string[] args)
    {
        var logger = new Logger<Program>(new LoggerFactory());
        var tokenCache = new TokenCache();
        using var host = Host.CreateDefaultBuilder(args)
            .ConfigureServices(
                (context, services) =>
                {
                    services.AddSingleton<ApiClient>(provider => new ApiClient(
                        tokenCache,
                        "http://localhost:5000/",
                        "object Object UA"
                    ));
                    services.AddSingleton<LocationsCache>();
                    services.AddSingleton<BlacklistCache>();
                    services.AddSingleton<DispatchService>();
                }
            )
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
            logger.LogWarning("Shutting down...");
            cts.Cancel();
            e.Cancel = true;
            if (count > 1)
            {
                logger.LogCritical("Forcefully shutting down...");
                Environment.Exit(0);
            }
        };

        var seed = "default";
        var targetDispatches = 50;
        var maxActiveCalls = 10;

        var dispatcher = host.Services.GetRequiredService<DispatchService>();
        var apiClient = host.Services.GetRequiredService<ApiClient>();

        try
        {
            await apiClient.PostRunReset(
                seed,
                targetDispatches,
                maxActiveCalls
            );

            var tokens = await apiClient.PostLogin(
                                            "distancify",
                                            "hackathon"
                                        );

            tokenCache.SetToken(tokens.Token);
            tokenCache.SetRefreshToken(tokens.RefreshToken);


        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during initialization");
            return;
        }

        try
        {
            await dispatcher.RunDispatcher(
                targetDispatches: 50,
                maxActiveCalls: 10,
                cancellationToken: cts.Token
            );
        }
        catch (OperationCanceledException)
        {
            logger.LogWarning("Operation was canceled");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred during dispatching");
        }
        finally
        {
            tokenCache.ClearTokens();
            await apiClient.PostRunStop();
            logger.LogInformation("Dispatcher stopped");
            Environment.Exit(0);
        }

        Console.WriteLine("Press any key to exit...");
        Console.ReadKey();
    }
}

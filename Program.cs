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
        var seed = Environment.GetEnvironmentVariable("SEED") ?? "default";
        var targetDispatches = int.Parse(Environment.GetEnvironmentVariable("TARGET_DISPATCHES") ?? "50");
        var maxActiveCalls = int.Parse(Environment.GetEnvironmentVariable("MAX_ACTIVE_CALLS") ?? "10");

        var logLevelEnv = Environment.GetEnvironmentVariable("LOG_LEVEL");
        var logLevel = logLevelEnv switch
        {
            "Debug" => LogLevel.Debug,
            "Information" => LogLevel.Information,
            "Warning" => LogLevel.Warning,
            "Error" => LogLevel.Error,
            "Critical" => LogLevel.Critical,
            _ => LogLevel.Information
        };

        var dispatchingDataEndpoint = Environment.GetEnvironmentVariable("DISPATCHING_DATA_ENDPOINT");
        if (string.IsNullOrEmpty(dispatchingDataEndpoint))
        {
            Console.WriteLine("DISPATCHING_DATA_ENDPOINT environment variable is not set.");
            return;
        }

        var logger = new Logger<Program>(new LoggerFactory());
        var tokenCache = new TokenCache();
        using var host = Host.CreateDefaultBuilder(args)
            .ConfigureServices(
                (context, services) =>
                {
                    services.AddSingleton<ApiClient>(provider => new ApiClient(
                        tokenCache,
                        dispatchingDataEndpoint,
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
                logging.SetMinimumLevel(logLevel);
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

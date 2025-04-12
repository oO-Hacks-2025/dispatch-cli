using EmergencyDispatcher.Api;
using EmergencyDispatcher.Cache;
using EmergencyDispatcher.Services;

namespace EmergencyDispatcher;

public class Program
{
    public static async Task Main(string[] args)
    {
        var configuration = LoadConfiguration(args);
        using var host = BuildHost(args, configuration);

        var logger = host.Services.GetRequiredService<ILogger<Program>>();
        var apiClient = host.Services.GetRequiredService<ApiClient>();
        var dispatcher = host.Services.GetRequiredService<DispatchService>();

        var cts = new CancellationTokenSource();
        SetupCancellation(cts, logger);

        try
        {
            await InitializeSystem(apiClient, configuration, logger);
            await RunDispatcher(dispatcher, logger, cts.Token);
        }
        catch (OperationCanceledException)
        {
            logger.LogWarning("Operation was canceled");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred during operation");
        }
        finally
        {
            await ShutdownSystem(apiClient, logger);
        }
    }

    private static (
        string Seed,
        int TargetDispatches,
        int MaxActiveCalls,
        LogLevel LogLevel,
        string Endpoint
    ) LoadConfiguration(string[] args)
    {
        var seed = Environment.GetEnvironmentVariable("SEED") ?? "default";
        var targetDispatches = int.Parse(
            Environment.GetEnvironmentVariable("TARGET_DISPATCHES") ?? "50"
        );
        var maxActiveCalls = int.Parse(
            Environment.GetEnvironmentVariable("MAX_ACTIVE_CALLS") ?? "10"
        );

        var logLevelEnv = Environment.GetEnvironmentVariable("LOG_LEVEL");
        var logLevel = logLevelEnv switch
        {
            "Debug" => LogLevel.Debug,
            "Information" => LogLevel.Information,
            "Warning" => LogLevel.Warning,
            "Error" => LogLevel.Error,
            "Critical" => LogLevel.Critical,
            _ => LogLevel.Information,
        };

        var dispatchingDataEndpoint =
            Environment.GetEnvironmentVariable("DISPATCHING_DATA_ENDPOINT")
            ?? "http://localhost:5000/";

        Console.WriteLine("Configuration:");
        Console.WriteLine($"Seed: {seed}");
        Console.WriteLine($"Target Dispatches: {targetDispatches}");
        Console.WriteLine($"Max Active Calls: {maxActiveCalls}");
        Console.WriteLine($"Log Level: {logLevel}");
        Console.WriteLine($"Dispatching Data Endpoint: {dispatchingDataEndpoint}");

        return (seed, targetDispatches, maxActiveCalls, logLevel, dispatchingDataEndpoint!);
    }

    private static IHost BuildHost(
        string[] args,
        (
            string Seed,
            int TargetDispatches,
            int MaxActiveCalls,
            LogLevel LogLevel,
            string Endpoint
        ) config
    )
    {
        return Host.CreateDefaultBuilder(args)
            .ConfigureServices(
                (context, services) =>
                {
                    services.AddSingleton<BlacklistCache>();
                    services.AddSingleton(provider => new ApiClient(
                        provider.GetRequiredService<ILogger<ApiClient>>(),
                        config.Endpoint,
                        "Emergency Dispatcher UA"
                    ));
                    services.AddSingleton<LocationsCache>();
                    services.AddSingleton<DispatchService>();
                }
            )
            .ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddConsole();
                logging.SetMinimumLevel(config.LogLevel);
            })
            .Build();
    }

    private static void SetupCancellation(CancellationTokenSource cts, ILogger<Program> logger)
    {
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
    }

    private static async Task InitializeSystem(
        ApiClient apiClient,
        (
            string Seed,
            int TargetDispatches,
            int MaxActiveCalls,
            LogLevel LogLevel,
            string Endpoint
        ) config,
        ILogger<Program> logger
    )
    {
        try
        {
            await apiClient.PostRunReset(
                config.Seed,
                config.TargetDispatches,
                config.MaxActiveCalls
            );
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during initialization");
            throw;
        }
    }

    private static async Task RunDispatcher(
        DispatchService dispatcher,
        ILogger<Program> logger,
        CancellationToken cancellationToken
    )
    {
        try
        {
            await dispatcher.RunDispatcher(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred during dispatching");
            throw;
        }
    }

    private static async Task ShutdownSystem(
        ApiClient apiClient,
        ILogger<Program> logger
    )
    {
        try
        {
            await apiClient.PostRunStop();
            logger.LogInformation("Dispatcher stopped");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during shutdown");
        }
        finally
        {
            Environment.Exit(0);
        }
    }
}

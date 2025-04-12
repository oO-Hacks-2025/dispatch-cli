using EmergencyDispatcher.Api;
using EmergencyDispatcher.Cache;
using EmergencyDispatcher.Services;

namespace EmergencyDispatcher;

public record AppConfig(
    string Seed,
    int TargetDispatches,
    int MaxActiveCalls,
    LogLevel LogLevel,
    string Endpoint
);

public class Program
{
    public static async Task Main(string[] args)
    {
        var configuration = ParseConfiguration(args);
        using var host = BuildHost(args, configuration);

        var logger = host.Services.GetRequiredService<ILogger<Program>>();
        var tokenCache = host.Services.GetRequiredService<TokenCache>();
        var apiClient = host.Services.GetRequiredService<ApiClient>();
        var dispatcher = host.Services.GetRequiredService<DispatchService>();

        var cts = new CancellationTokenSource();
        SetupCancellation(cts, logger);

        try
        {
            await InitializeSystem(apiClient, tokenCache, configuration, logger);
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
            await ShutdownSystem(apiClient, tokenCache, logger);
        }
    }

    private static AppConfig ParseConfiguration(string[] args)
    {
        // Display help if requested
        if (args.Contains("--help") || args.Contains("-h") || args.Contains("-?"))
        {
            DisplayHelp();
            Environment.Exit(0);
        }

        // Build configuration with command line arguments
        var configuration = new ConfigurationBuilder()
            .AddEnvironmentVariables()
            .AddCommandLine(
                args,
                new Dictionary<string, string>
                {
                    { "-s", "Seed" },
                    { "--seed", "Seed" },
                    { "-t", "TargetDispatches" },
                    { "--target-dispatches", "TargetDispatches" },
                    { "-m", "MaxActiveCalls" },
                    { "--max-active-calls", "MaxActiveCalls" },
                    { "-l", "LogLevel" },
                    { "--log-level", "LogLevel" },
                    { "-e", "Endpoint" },
                    { "--endpoint", "Endpoint" },
                }
            )
            .Build();

        // Set defaults and override with environment vars and command line args
        var seed = configuration["Seed"] ?? "default";

        var targetDispatchesStr = configuration["TargetDispatches"];
        var targetDispatches = 50;
        if (
            !string.IsNullOrEmpty(targetDispatchesStr)
            && int.TryParse(targetDispatchesStr, out int parsedTargetDispatches)
        )
        {
            targetDispatches = parsedTargetDispatches;
        }

        var maxActiveCallsStr = configuration["MaxActiveCalls"];
        var maxActiveCalls = 10;
        if (
            !string.IsNullOrEmpty(maxActiveCallsStr)
            && int.TryParse(maxActiveCallsStr, out int parsedMaxActiveCalls)
        )
        {
            maxActiveCalls = parsedMaxActiveCalls;
        }

        var logLevelStr = configuration["LogLevel"];
        var logLevel = LogLevel.Information;
        if (!string.IsNullOrEmpty(logLevelStr))
        {
            logLevel = ParseLogLevel(logLevelStr);
        }

        var endpoint = configuration["Endpoint"] ?? configuration["DISPATCHING_DATA_ENDPOINT"];
        if (string.IsNullOrEmpty(endpoint))
        {
            Console.WriteLine(
                "Error: Dispatching data endpoint is required. Provide it via environment variable DISPATCHING_DATA_ENDPOINT or --endpoint flag."
            );
            Environment.Exit(1);
        }

        return new AppConfig(seed, targetDispatches, maxActiveCalls, logLevel, endpoint);
    }

    private static void DisplayHelp()
    {
        Console.WriteLine("Emergency Dispatcher Service");
        Console.WriteLine("\nUsage:");
        Console.WriteLine("  dotnet run [options]");
        Console.WriteLine("\nOptions:");
        Console.WriteLine(
            "  -s, --seed <VALUE>                Seed for the simulation (default: \"default\")"
        );
        Console.WriteLine(
            "  -t, --target-dispatches <NUMBER>  Target number of dispatches (default: 50)"
        );
        Console.WriteLine(
            "  -m, --max-active-calls <NUMBER>   Maximum number of active calls (default: 10)"
        );
        Console.WriteLine(
            "  -l, --log-level <LEVEL>           Log level: Debug, Information, Warning, Error, Critical (default: Information)"
        );
        Console.WriteLine(
            "  -e, --endpoint <URL>              Dispatching data endpoint URL (required if DISPATCHING_DATA_ENDPOINT env var not set)"
        );
        Console.WriteLine("  -h, --help                        Display help");
        Console.WriteLine("\nEnvironment Variables:");
        Console.WriteLine("  SEED                              Same as --seed");
        Console.WriteLine("  TARGET_DISPATCHES                 Same as --target-dispatches");
        Console.WriteLine("  MAX_ACTIVE_CALLS                  Same as --max-active-calls");
        Console.WriteLine("  LOG_LEVEL                         Same as --log-level");
        Console.WriteLine("  DISPATCHING_DATA_ENDPOINT         Same as --endpoint");
    }

    private static LogLevel ParseLogLevel(string logLevelStr) =>
        logLevelStr.ToLowerInvariant() switch
        {
            "debug" => LogLevel.Debug,
            "information" or "info" => LogLevel.Information,
            "warning" or "warn" => LogLevel.Warning,
            "error" => LogLevel.Error,
            "critical" or "crit" => LogLevel.Critical,
            _ => LogLevel.Information,
        };

    private static IHost BuildHost(string[] args, AppConfig config)
    {
        return Host.CreateDefaultBuilder(args)
            .ConfigureServices(
                (context, services) =>
                {
                    services.AddSingleton<TokenCache>();
                    services.AddSingleton<BlacklistCache>();
                    services.AddSingleton(provider => new ApiClient(
                        provider.GetRequiredService<TokenCache>(),
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
        TokenCache tokenCache,
        AppConfig config,
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

            var tokens = await apiClient.PostLogin("distancify", "hackathon");
            tokenCache.SetToken(tokens.Token);
            tokenCache.SetRefreshToken(tokens.RefreshToken);
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
        TokenCache tokenCache,
        ILogger<Program> logger
    )
    {
        try
        {
            tokenCache.ClearTokens();
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

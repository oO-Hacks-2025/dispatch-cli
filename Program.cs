using Cache;
using System.Net.Http.Headers;
using System.Text.Json;
using testing.ApiClient;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureLogging(logging =>
    {
        logging.ClearProviders();
        logging.AddConsole();
        logging.SetMinimumLevel(LogLevel.Debug);
    })
    .Build();

ILogger<Program> logger = host.Services.GetRequiredService<ILogger<Program>>();
ILogger<LocationsCache> cacheLogger = host.Services.GetRequiredService<ILogger<LocationsCache>>();
ILogger<DispatchService> dispatchLogger = host.Services.GetRequiredService<ILogger<DispatchService>>();

var apiClient = new ApiClient();

logger.LogInformation("HTTP client configured.");

var cache = new LocationsCache(apiClient, cacheLogger);

var cacheGenerated = await cache.GenerateCache();

DispatchService dispatchService = new DispatchService(apiClient, cache, dispatchLogger);

var emergencyCall = await apiClient.GetCallNext();
await dispatchService.Dispatch(emergencyCall);

var result = await apiClient.GetRunStatus();

logger.LogInformation(JsonSerializer.Serialize(result));

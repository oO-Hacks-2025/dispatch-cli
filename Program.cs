using Cache;
using System.Net.Http.Headers;
using System.Text.Json;
using testing.ApiClient;
using testing.Models;
using System.Reactive.Linq;
using System.Reactive.Subjects;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureLogging(logging =>
    {
        logging.ClearProviders();
        logging.AddConsole();
        logging.SetMinimumLevel(LogLevel.Debug);
    })
    .Build();

ILogger<Program> logger = host.Services.GetRequiredService<ILogger<Program>>();
ILogger<LocationsCache> locationsCacheLogger = host.Services.GetRequiredService<ILogger<LocationsCache>>();
ILogger<BlacklistCache> blacklistCacheLogger = host.Services.GetRequiredService<ILogger<BlacklistCache>>();
ILogger<DispatchService> dispatchLogger = host.Services.GetRequiredService<ILogger<DispatchService>>();

var apiClient = new ApiClient();

logger.LogInformation("HTTP client configured.");

var locationsCache = new LocationsCache(apiClient, locationsCacheLogger);
var blacklistCache = new BlacklistCache(blacklistCacheLogger);
var dispatchService = new DispatchService(apiClient, locationsCache, blacklistCache, dispatchLogger);

var cacheGenerated = await locationsCache.GenerateCache();

var targetDispatches = 10_000;
var maxActiveCalls = 100;
var completedDispatches = 0;


logger.LogInformation("Locations cache generated.");


await apiClient.PostRunReset("default", targetDispatches, maxActiveCalls);
logger.LogInformation("Run was reset successfully.");

var emergencyCall = await apiClient.GetCallNext();
await dispatchService.Dispatch(emergencyCall);

// while completedDispatches < targetDispatches
// {
//     if (activeCalls.Count < maxActiveCalls) {
//          var emergencyCall = await apiClient.GetCallNext();
//          if (emergencyCall is null)
//     {
//         break;
//     }
//     }
//    
//     activeCalls.Add(emergencyCall);
//
//     const dispatched = await dispatchService.Dispatch(emergencyCall);
//
//     if (!dispatched)
//        continue;
//     
//     completedDispatches++;
//     logger.LogInformation($"Dispatched {completedDispatches} out of {targetDispatches} calls.");
//     logger.LogInformation($"Active calls: {activeCalls.Count}.");
// }

await apiClient.PostRunStop();
logger.LogInformation("Run was stopped successfully.");
var result = await apiClient.GetRunStatus();

logger.LogInformation(JsonSerializer.Serialize(result));

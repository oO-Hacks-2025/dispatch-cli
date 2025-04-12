using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Collections.Concurrent;
using Cache;
using testing.ApiClient;
using testing.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;

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
var locationsCache = new LocationsCache(apiClient, locationsCacheLogger);
var blacklistCache = new BlacklistCache(blacklistCacheLogger);
var dispatchService = new DispatchService(apiClient, locationsCache, blacklistCache, dispatchLogger);

int targetDispatches = 50;
int maxActiveCalls = 10;
int completedDispatches = 0;
bool runStopped = false;

await apiClient.PostRunReset("default", targetDispatches, maxActiveCalls);
await locationsCache.GenerateCache();

var semaphore = new SemaphoreSlim(maxActiveCalls);
var cts = new CancellationTokenSource();
var dispatchTasks = new ConcurrentBag<Task>();
var skippedCalls = new ConcurrentDictionary<string, bool>();

async Task StopRunOnceAsync()
{
    if (!runStopped)
    {
        runStopped = true;
        await apiClient.PostRunStop();
        logger.LogInformation("Run was stopped successfully.");
        var result = await apiClient.GetRunStatus();
        logger.LogInformation(JsonSerializer.Serialize(result));
    }
}
async Task DispatchLoop()
{
    while (!cts.Token.IsCancellationRequested && Interlocked.CompareExchange(ref completedDispatches, 0, 0) < targetDispatches)
    {
        await semaphore.WaitAsync();

        Call call = null;
        int attempts = 0;

        while (attempts < 5)
        {
            call = await apiClient.GetCallNext();

            if (call == null)
            {
                await Task.Delay(100);
                break;
            }

            var callKey = $"{call.City}::{call.County}";

            if (!skippedCalls.ContainsKey(callKey))
            {
                break;
            }

            call = null;
            attempts++;
        }

        if (call == null)
        {
            semaphore.Release();
            continue;
        }

        var task = Task.Run(async () =>
        {
            try
            {
                bool success = await dispatchService.Dispatch(call);

                if (!success)
                {
                    logger.LogWarning("Dispatch failed or was skipped for call.");
                    skippedCalls.TryAdd($"{call.City}::{call.County}", true);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error dispatching call.");
                await StopRunOnceAsync();
                cts.Cancel();
                return;
            }
            finally
            {
                Interlocked.Increment(ref completedDispatches); // ✅ Always increment
                semaphore.Release();
            }
        });

        dispatchTasks.Add(task);
    }
}

var dispatcherTask = DispatchLoop();

// Monitor progress and shut down when completed
while (Interlocked.CompareExchange(ref completedDispatches, 0, 0) < targetDispatches)
{
    await Task.Delay(500);
}

cts.Cancel();
await dispatcherTask;
await Task.WhenAll(dispatchTasks);

// Ensure the run is stopped once
await StopRunOnceAsync();

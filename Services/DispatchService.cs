using System.Collections.Concurrent;
using Cache;
using testing.ApiClient;
using testing.Models;

namespace testing.Services;

public class DispatchService(
    ApiClient.ApiClient client,
    LocationsCache locationsCache,
    BlacklistCache blacklistCache,
    ILogger<DispatchService> logger
)
{
    private readonly ApiClient.ApiClient _client = client;
    private readonly LocationsCache _locationsCache = locationsCache;
    private readonly BlacklistCache _blacklistCache = blacklistCache;
    private readonly ILogger<DispatchService> _logger = logger;
    private readonly SemaphoreSlim _apiSemaphore = new(10);
    private readonly ConcurrentDictionary<string, int> _availabilityCache = new();

    public async Task RunDispatcher(
        CancellationToken cancellationToken = default
    )
    {
        _logger.LogInformation("Starting emergency dispatch service");

        await _locationsCache.GenerateCache();

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                Call? call = null;
                try
                {
                    call =
                        await _client.GetCallNext()
                        ?? throw new EmptyQueueException("No calls in queue");
                }
                catch (EmptyQueueException)
                {
                    var finalStatus = await _client.PostRunStop();
                    LogFinalResults(finalStatus!);
                    _logger.LogInformation("No more calls in queue. Stopping dispatcher.");
                    cancellationToken.ThrowIfCancellationRequested();
                    Environment.Exit(0);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error getting next call");
                    continue;
                }

                await ProcessCallAsync(call, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in main dispatch loop");
                await Task.Delay(100, cancellationToken);
            }
        }

        await ProcessQueuedCallsAsync(cancellationToken);
    }

    private async Task ProcessQueuedCallsAsync(CancellationToken cancellationToken)
    {
        try
        {
            var callQueue = await _client.GetCallQueue();
            if (callQueue == null || callQueue.Count == 0)
            {
                return;
            }

            _logger.LogInformation($"Processing {callQueue.Count} queued calls");

            var tasks = new List<Task>();
            foreach (var call in callQueue)
            {
                tasks.Add(ProcessCallAsync(call, cancellationToken));
            }

            await Task.WhenAll(tasks);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing queued calls");
        }
    }

    private async Task ProcessCallAsync(Call call, CancellationToken cancellationToken)
    {
        if (call?.Requests == null || call.Requests.Count == 0)
        {
            return;
        }

        _logger.LogInformation(
            $"Processing call for {call.City}, {call.County} with {call.Requests.Count} requests"
        );

        List<CacheItem> locations;
        try
        {
            var key = $"{call.City}::{call.County}";
            locations = _locationsCache.GetCacheItem(key);
        }
        catch (KeyNotFoundException)
        {
            _logger.LogWarning($"No locations found for {call.City}, {call.County}");
            return;
        }

        var tasks = call.Requests.Select(request =>
            ProcessServiceRequestAsync(request, call, locations, cancellationToken)
        );

        await Task.WhenAll(tasks);
    }

    private async Task ProcessServiceRequestAsync(
        ServiceRequest request,
        Call call,
        List<CacheItem> locations,
        CancellationToken cancellationToken
    )
    {
        if (request == null || request.Quantity <= 0)
        {
            return;
        }

        _logger.LogInformation(
            $"Processing {request.ServiceType} request for {call.City}, {call.County} - Quantity: {request.Quantity}"
        );

        int remainingQuantity = request.Quantity;
        int locationIndex = 0;

        while (
            remainingQuantity > 0
            && locationIndex < locations.Count
            && !cancellationToken.IsCancellationRequested
        )
        {
            var location = locations[locationIndex];
            string locationKey = $"{location.City}::{location.County}";

            if (_blacklistCache.IsBlacklisted(request.ServiceType, locationKey))
            {
                locationIndex++;
                continue;
            }

            int availableQuantity;
            try
            {
                await _apiSemaphore.WaitAsync(cancellationToken);

                try
                {
                    availableQuantity = await _client.GetServiceAvailabilityByCity(
                        request.ServiceType,
                        location.County,
                        location.City
                    );

                    if (availableQuantity <= 0)
                    {
                        _blacklistCache.BlacklistLocation(
                            location.City,
                            location.County,
                            request.ServiceType
                        );
                        locationIndex++;
                        continue;
                    }
                }
                finally
                {
                    _apiSemaphore.Release();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    $"Error checking availability in {location.City}, {location.County}"
                );
                locationIndex++;
                continue;
            }

            int dispatchQuantity = Math.Min(availableQuantity, remainingQuantity);

            try
            {
                await _apiSemaphore.WaitAsync(cancellationToken);

                try
                {
                    int confirmedQuantity = await _client.GetServiceAvailabilityByCity(
                        request.ServiceType,
                        location.County,
                        location.City
                    );

                    if (confirmedQuantity <= 0)
                    {
                        _blacklistCache.BlacklistLocation(
                            location.City,
                            location.County,
                            request.ServiceType
                        );
                        locationIndex++;
                        continue;
                    }

                    dispatchQuantity = Math.Min(confirmedQuantity, dispatchQuantity);

                    await _client.PostServiceDispatch(
                        location.County,
                        location.City,
                        call.County,
                        call.City,
                        dispatchQuantity,
                        request.ServiceType
                    );

                    _logger.LogInformation(
                        $"Dispatched {dispatchQuantity} {request.ServiceType} from {location.City}, {location.County} to {call.City}, {call.County}"
                    );

                    remainingQuantity -= dispatchQuantity;

                    if (confirmedQuantity <= dispatchQuantity)
                    {
                        _blacklistCache.BlacklistLocation(
                            location.City,
                            location.County,
                            request.ServiceType
                        );
                    }
                }
                finally
                {
                    _apiSemaphore.Release();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error dispatching from {location.City}, {location.County}");
            }

            locationIndex++;
        }

        if (remainingQuantity > 0)
        {
            _logger.LogWarning(
                $"Could not fulfill entire request for {request.ServiceType}. Remaining: {remainingQuantity}"
            );
        }
    }

    private void LogFinalResults(GameStatus status)
    {
        if (status == null)
            return;

        _logger.LogInformation("======== FINAL RESULTS ========");
        _logger.LogInformation($"Status: {status.Status}");
        _logger.LogInformation(
            $"Total dispatches: {status.TotalDispatches}/{status.TargetDispatches}"
        );
        _logger.LogInformation($"Running time: {status.RunningTime}");
        _logger.LogInformation($"Distance: {status.Distance}");
        _logger.LogInformation($"Penalty: {status.Penalty}");
        _logger.LogInformation($"HTTP Requests: {status.HttpRequests}");
        _logger.LogInformation($"Errors - Missed: {status.Error.Missed}");
        _logger.LogInformation($"Errors - Over-dispatched: {status.Error.OverDispatched}");
        _logger.LogInformation("==============================");
    }
}

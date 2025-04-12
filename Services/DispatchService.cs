using EmergencyDispatcher.Api;
using EmergencyDispatcher.Cache;
using EmergencyDispatcher.Domain.Models;

namespace EmergencyDispatcher.Services;

public class DispatchService(
    ApiClient client,
    LocationsCache locationsCache,
    BlacklistCache blacklistCache,
    ILogger<DispatchService> logger
)
{
    private readonly ApiClient _client = client;
    private readonly LocationsCache _locationsCache = locationsCache;
    private readonly BlacklistCache _blacklistCache = blacklistCache;
    private readonly ILogger<DispatchService> _logger = logger;
    private readonly SemaphoreSlim _apiSemaphore = new(10);

    public async Task RunDispatcher(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting emergency dispatch service");

        await _locationsCache.GenerateCache();

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                Call? call = await GetNextCall(cancellationToken);
                await ProcessCallAsync(call, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                _logger.LogInformation("Dispatcher operation canceled");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in main dispatch loop");
                await Task.Delay(100, cancellationToken);
            }
        }
    }

    private async Task<Call> GetNextCall(CancellationToken cancellationToken)
    {
        try
        {
            var call = await _client.GetCallNext();
            if (call == null)
            {
                var finalStatus = await _client.PostRunStop();
                LogFinalResults(finalStatus!);
                _logger.LogInformation("No more calls in queue. Stopping dispatcher.");
                cancellationToken.ThrowIfCancellationRequested();
                Environment.Exit(0);
                throw new EmptyQueueException("No calls in queue");
            }
            return call;
        }
        catch (EmptyQueueException)
        {
            var finalStatus = await _client.PostRunStop();
            LogFinalResults(finalStatus!);
            _logger.LogInformation("No more calls in queue. Stopping dispatcher.");
            cancellationToken.ThrowIfCancellationRequested();
            Environment.Exit(0);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting next call");
            throw;
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

            try
            {
                int availableQuantity = await CheckAvailability(
                    request.ServiceType,
                    location,
                    cancellationToken
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

                int confirmedQuantity = await CheckAvailability(
                    request.ServiceType,
                    location,
                    cancellationToken
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

                int dispatchQuantity = Math.Min(confirmedQuantity, remainingQuantity);
                await DispatchServices(
                    request.ServiceType,
                    location,
                    call,
                    dispatchQuantity,
                    cancellationToken
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
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    $"Error processing location {location.City}, {location.County}"
                );
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

    private async Task<int> CheckAvailability(
        ServiceType serviceType,
        CacheItem location,
        CancellationToken cancellationToken
    )
    {
        await _apiSemaphore.WaitAsync(cancellationToken);
        try
        {
            return await _client.GetServiceAvailabilityByCity(
                serviceType,
                location.County,
                location.City
            );
        }
        finally
        {
            _apiSemaphore.Release();
        }
    }

    private async Task DispatchServices(
        ServiceType serviceType,
        CacheItem source,
        Call target,
        int quantity,
        CancellationToken cancellationToken
    )
    {
        await _apiSemaphore.WaitAsync(cancellationToken);
        try
        {
            await _client.PostServiceDispatch(
                source.County,
                source.City,
                target.County,
                target.City,
                quantity,
                serviceType
            );

            _logger.LogInformation(
                $"Dispatched {quantity} {serviceType} from {source.City}, {source.County} to {target.City}, {target.County}"
            );
        }
        finally
        {
            _apiSemaphore.Release();
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

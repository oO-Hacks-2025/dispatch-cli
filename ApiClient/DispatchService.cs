using System.Text.Json;
using Cache;
using Microsoft.Extensions.Logging;
using testing.Models;

namespace testing.ApiClient
{
    public class DispatchService
    {
        private readonly ApiClient _client;
        private readonly LocationsCache _locationsCache;
        private readonly BlacklistCache _blacklistCache;
        private readonly ILogger<DispatchService> _logger;

        public DispatchService(ApiClient client, LocationsCache locationsCache, BlacklistCache blacklistCache, ILogger<DispatchService> logger)
        {
            _client = client;
            _locationsCache = locationsCache;
            _blacklistCache = blacklistCache;
            _logger = logger;
        }

        public async Task<bool> Dispatch(Call emergencyCall)
        {
            if (emergencyCall == null || emergencyCall.Requests == null)
            {
                _logger.LogWarning("Invalid emergency call received.");
                return false;
            }

            _logger.LogInformation($"Dispatching emergency call in {emergencyCall.City}, {emergencyCall.County}...");

            var locations = _locationsCache.GetCacheItem($"{emergencyCall.City}::{emergencyCall.County}");

            if (locations == null || locations.Count == 0)
            {
                _logger.LogWarning($"No cached locations available for {emergencyCall.City}, {emergencyCall.County}.");
                return false;
            }

            bool anyDispatched = false;

            foreach (var request in emergencyCall.Requests)
            {
                if (request == null || request.Quantity <= 0)
                {
                    _logger.LogWarning("Skipped invalid request in emergency call.");
                    continue;
                }

                int requestQuantity = request.Quantity;
                int sourceCityIndex = 0;

                _logger.LogInformation($"Processing request: {request.ServiceType} x{request.Quantity}");

                while (requestQuantity > 0 && sourceCityIndex < locations.Count)
                {
                    var location = locations[sourceCityIndex];
                    string locationKey = $"{location.City}::{location.County}";

                    if (_blacklistCache.IsBlacklisted(request.ServiceType, locationKey))
                    {
                        _logger.LogDebug($"Skipping blacklisted location: {locationKey} for {request.ServiceType}");
                        sourceCityIndex++;
                        continue;
                    }

                    int availableQty;
                    try
                    {
                        availableQty = await _client.GetServiceAvailabilityByCity(
                            (ServiceType)request.ServiceType, location.County, location.City);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning($"Failed to get availability from {locationKey}: {ex.Message}");
                        sourceCityIndex++;
                        continue;
                    }

                    if (availableQty <= 0)
                    {
                        _logger.LogWarning($"Invalid or zero availability ({availableQty}) for {request.ServiceType} in {locationKey}");
                        sourceCityIndex++;
                        continue;
                    }

                    int dispatchQty = Math.Min(requestQuantity, availableQty);

                    try
                    {
                        await _client.PostServiceDispatch(
                            location.County, location.City,
                            emergencyCall.County, emergencyCall.City,
                            dispatchQty, request.ServiceType);

                        requestQuantity -= dispatchQty;
                        anyDispatched = true;

                        _logger.LogInformation(
                            $"[Emergency::{emergencyCall.City}::{emergencyCall.County}] Dispatched {dispatchQty} of {request.ServiceType} " +
                            $"from {location.City} in {location.County} to {emergencyCall.City} in {emergencyCall.County}");

                        if (availableQty == dispatchQty)
                        {
                            _blacklistCache.BlacklistLocation(location.City, location.County, request.ServiceType);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning($"Failed to dispatch {dispatchQty} of {request.ServiceType} from {locationKey}: {ex.Message}");
                        break;
                    }

                    sourceCityIndex++;
                }

                if (requestQuantity > 0)
                {
                    _logger.LogWarning($"Could not fully fulfill request: {request.ServiceType} - Remaining: {requestQuantity}");
                }
            }
            bool allRequestsFulfilled = emergencyCall.Requests.All(r => r == null || r.Quantity <= 0);

            if (!anyDispatched || !allRequestsFulfilled)
            {
                _logger.LogWarning($"Could not fully fulfill emergency call in {emergencyCall.City}, {emergencyCall.County}. Skipping future attempts.");
                return false;
            }


            return true;
        }
    }
}

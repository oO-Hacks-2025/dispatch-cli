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
            _logger.LogInformation($"Dispatching service for {emergencyCall.City} in {emergencyCall.County}...");

            if (emergencyCall is null || emergencyCall.Requests is null)
            {
                return false;
            }

            var locations = _locationsCache.GetCacheItem(string.Concat(emergencyCall.City, "::", emergencyCall.County));

            if (locations is null || locations.Count == 0)
            {
                return false;
            }

            _logger.LogError($"{JsonSerializer.Serialize(emergencyCall.Requests)}");

            foreach (var request in emergencyCall.Requests)
            {
                _logger.LogInformation($"Processing request for {request.ServiceType} with quantity {request.Quantity}...");

                if (request is null)
                {
                    continue;
                }
                var requestQuantity = request.Quantity;
                var sourceCityIndex = 0;

                while (requestQuantity > 0 && sourceCityIndex < locations.Count)
                {
                    var location = locations[sourceCityIndex];

                    if (_blacklistCache.IsBlacklisted(request.ServiceType, $"{location.City}::{location.County}"))
                    {
                        sourceCityIndex++;
                        continue;
                    }

                    var availableQty = await _client.GetServiceAvailabilityByCity((ServiceType)request.ServiceType, location.County, location.City);

                    if (availableQty >= requestQuantity)
                    {
                        await _client.PostServiceDispatch(location.County,
                                               location.City,
                                               emergencyCall.County,
                                               emergencyCall.City,
                                               request.Quantity,
                                               request.ServiceType);

                        if (availableQty == requestQuantity)
                        {
                            _blacklistCache.BlacklistLocation(location.City, location.County, request.ServiceType);
                        }

                        requestQuantity = 0;
                    }

                    if (availableQty > 0 && availableQty < requestQuantity)
                    {
                        await _client.PostServiceDispatch(location.County,
                                               location.City,
                                               emergencyCall.County,
                                               emergencyCall.City,
                                               availableQty,
                                               request.ServiceType);

                        requestQuantity -= availableQty;

                        _blacklistCache.BlacklistLocation(location.City, location.County, request.ServiceType);
                    }

                    if (availableQty == 0)
                    {
                        sourceCityIndex++;
                    }

                    _logger.LogInformation($"[Emergency::{emergencyCall.City}::{emergencyCall.County}] Dispatched {request.Quantity} of {request.ServiceType} from {location.City} in {location.County} to {emergencyCall.City} in {emergencyCall.County}");

                    return true;
                }
            }

            return false;
        }
    }
}

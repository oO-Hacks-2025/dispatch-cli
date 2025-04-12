using System.Text.Json;
using Cache;
using testing.Models;

namespace testing.ApiClient
{
    public class DispatchService
    {
        private readonly ApiClient _client;
        private readonly LocationsCache _locationsCache;
        private readonly ILogger<DispatchService> _logger;

        public DispatchService(ApiClient client, LocationsCache locationsCache, ILogger<DispatchService> logger)
        {
            _client = client;
            _locationsCache = locationsCache;
            _logger = logger;
        }

        public async Task Dispatch(Call emergencyCall)
        {
            _logger.LogInformation($"Dispatching service for {emergencyCall.City} in {emergencyCall.County}...");

            if (emergencyCall is null || emergencyCall.Requests is null)
            {
                return;
            }

            var locations = _locationsCache.GetCacheItem(string.Concat(emergencyCall.City, "::", emergencyCall.County));

            if (locations is null || locations.Count == 0)
            {
                return;
            }

            _logger.LogError($"{JsonSerializer.Serialize(emergencyCall.Requests)}");

            foreach (var request in emergencyCall.Requests)
            {
                _logger.LogInformation($"Processing request for {request.ServiceType} with quantity {request.Quantity}...");

                if (request is null)
                {
                    continue;
                }
                var remainingQty = request.Quantity;
                var sourceCityIndex = 0;

                while (remainingQty > 0 && sourceCityIndex < locations.Count)
                {
                    var location = locations[sourceCityIndex];

                    var availableQty = await _client.GetServiceAvailabilityByCity((ServiceType)request.ServiceType, location.County, location.City);

                    if (availableQty >= remainingQty)
                    {
                        await _client.PostServiceDispatch(location.County,
                                               location.City,
                                               emergencyCall.County,
                                               emergencyCall.City,
                                               request.Quantity,
                                               request.ServiceType);

                        remainingQty = 0;
                    }

                    if (availableQty > 0 && availableQty < remainingQty)
                    {
                        await _client.PostServiceDispatch(location.County,
                                               location.City,
                                               emergencyCall.County,
                                               emergencyCall.City,
                                               availableQty,
                                               request.ServiceType);

                        remainingQty -= availableQty;
                    }

                    if (availableQty == 0)
                    {
                        sourceCityIndex++;
                    }

                    _logger.LogInformation($"[Emergency::{emergencyCall.City}::{emergencyCall.County}] Dispatched {request.Quantity} of {request.ServiceType} from {location.City} in {location.County} to {emergencyCall.City} in {emergencyCall.County}");

                }
            }
        }
    }
}

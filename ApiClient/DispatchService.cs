using Cache;
using testing.Models;

namespace testing.ApiClient
{
    public class DispatchService
    {
        private readonly ApiClient _client;
        private readonly LocationsCache _locationsCache;

        public DispatchService(ApiClient client, LocationsCache locationsCache)
        {
            _client = client;
            _locationsCache = locationsCache;
        }

        public async Task Dispatch()
        {


            Call emergencyCall;
            try
            {
                emergencyCall = await _client.GetCallNext();

            }
            catch (Exception ex)
            {
                return;

            }

            if (emergencyCall is null || emergencyCall.Requests is null)
            {
                return;
            }

            var locations = _locationsCache.GetCacheItem(string.Concat(emergencyCall.City, "::", emergencyCall.County));

            if (locations is null || locations.Count == 0)
            {
                return;
            }

            foreach (var request in emergencyCall.Requests)
            {
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
                        return;
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

                    sourceCityIndex++;
                }
            }
        }
    }
}

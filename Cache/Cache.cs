using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using testing.ApiClient;
using testing.Models;

namespace Cache;

public class LocationsCache(ApiClient clientService, ILogger<LocationsCache> logger)
{
    ApiClient _clientService = clientService;
    private readonly IMemoryCache _cache = new MemoryCache(new MemoryCacheOptions());
    private readonly ILogger<LocationsCache> _logger = logger;
    private List<string> _availabilites = [
        ServiceType.Fire.ToString(),
        ServiceType.Police.ToString(),
        ServiceType.Medical.ToString(),
        ServiceType.Rescue.ToString(),
        ServiceType.Utility.ToString()
    ];

    private async Task<List<City>> _getLocations()
    {
        var locations = await _clientService.Locations();
        return locations;
    }

    private async Task<List<Availability>> _getAvailabilities()
    {
        var availabilityBatches = await Task.WhenAll(
            _clientService.Search(ServiceType.Fire),
            _clientService.Search(ServiceType.Police),
            _clientService.Search(ServiceType.Medical),
            _clientService.Search(ServiceType.Rescue),
            _clientService.Search(ServiceType.Utility)
        );

        return [.. availabilityBatches.SelectMany(availability => availability)];
    }

    private async Task<List<Availability>> _getMunicipalities()
    {
        return await _clientService.Search(ServiceType.Medical);
    }

    public async Task<bool> GenerateCache()
    {
        _logger.LogDebug("Generating distances cache...");

        _logger.LogDebug("Fetching required data for distances cache generaiton...");
        var locations = await _getLocations();

        _logger.LogDebug("Building distances cache...");

        var countyMunicipalities = await _getMunicipalities();

        foreach (var target in locations)
        {
            var key = _buildKey(target.Name, target.County);
            var listOfSources = new List<CacheItem>();

            foreach (var source in countyMunicipalities)
            {
                if (source.City == target.Name && source.County == target.County)
                {
                    continue;
                }

                var distance = ComputeDistance(target, source);

                listOfSources.Add(new CacheItem
                {
                    City = source.City,
                    County = source.County,
                    Distance = distance,
                });
            }

            listOfSources.Sort((first, second) =>
            {
                if (first.Distance == second.Distance)
                {
                    return 0;
                }
                return first.Distance < second.Distance ? -1 : 1;
            });

            _cache.Set(key, listOfSources);
        }

        _logger.LogDebug("Distances cache successfully generated.");
        return true;
    }

    private static double ComputeDistance(City target, Availability source)
    {
        return Math.Sqrt(Math.Pow(target.Lat - source.Latitude, 2) + Math.Pow(target.Long - source.Longitude, 2));
    }

    private static string _buildKey(string city, string county)
    {
        return $"{city}::{county}";
    }

    public List<CacheItem> GetCacheItem(string key)
    {
        if (_cache.TryGetValue(key, out var value))
        {
            return value as List<CacheItem> ?? new List<CacheItem>();
        }

        throw new KeyNotFoundException($"Key '{key}' not found in cache.");
    }

    public async Task<List<City>> GetTestVals()
    {
        var locations = await _getLocations();
        return locations;
    }
}

public class CacheItem
{
    public string City { get; set; }
    public string County { get; set; }
    public double Distance { get; set; }
}
using EmergencyDispatcher.Api;
using EmergencyDispatcher.Domain.Models;
using Microsoft.Extensions.Caching.Memory;

namespace EmergencyDispatcher.Cache;

public class LocationsCache(ApiClient clientService, ILogger<LocationsCache> logger)
{
    private readonly ApiClient _clientService = clientService;
    private readonly MemoryCache _cache = new(new MemoryCacheOptions());
    private readonly ILogger<LocationsCache> _logger = logger;

    private async Task<List<City>> GetLocations()
    {
        var locations = await _clientService.GetLocations();
        return locations ?? throw new Exception("Failed to fetch locations from API.");
    }

    private async Task<List<Availability>> GetMunicipalities()
    {
        return await _clientService.GetServiceAvailability(ServiceType.Medical)
            ?? throw new Exception("Failed to fetch municipalities from API.");
    }

    public async Task<bool> GenerateCache()
    {
        _logger.LogDebug("Generating locations cache...");

        _logger.LogDebug("Fetching required data for cache generation...");

        var locations = await GetLocations();
        var municipalities = await GetMunicipalities();

        _logger.LogDebug("Building distances cache...");

        foreach (var target in locations)
        {
            var key = BuildKey(target.Name, target.County);
            var listOfSources = new List<CacheItem>();

            foreach (var source in municipalities)
            {
                if (source.City == target.Name && source.County == target.County)
                {
                    continue;
                }

                var distance = ComputeDistance(target, source);

                listOfSources.Add(
                    new CacheItem
                    {
                        City = source.City,
                        County = source.County,
                        Distance = distance,
                    }
                );
            }

            listOfSources.Sort((first, second) => first.Distance.CompareTo(second.Distance));

            _cache.Set(key, listOfSources);
        }

        _logger.LogDebug("Locations cache successfully generated");
        return true;
    }

    private static double ComputeDistance(City target, Availability source)
    {
        return Math.Sqrt(
            Math.Pow(target.Lat - source.Latitude, 2) + Math.Pow(target.Long - source.Longitude, 2)
        );
    }

    private static string BuildKey(string city, string county)
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
}

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using testing.ApiClient;
using testing.Models;

namespace Cache;

public class LocationsCache
{
    private readonly ApiClient _clientService;
    private readonly IMemoryCache _cache = new MemoryCache(new MemoryCacheOptions());
    private readonly ILogger<LocationsCache> _logger;
    private readonly List<string> _serviceTypes =
    [
        ServiceType.Fire.ToString(),
        ServiceType.Police.ToString(),
        ServiceType.Medical.ToString(),
        ServiceType.Rescue.ToString(),
        ServiceType.Utility.ToString(),
    ];

    public LocationsCache(ApiClient clientService, ILogger<LocationsCache> logger)
    {
        _clientService = clientService;
        _logger = logger;
    }

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
        _logger.LogInformation("Generating locations cache...");

        _logger.LogInformation("Fetching required data for cache generation...");

        var locations = await GetLocations();
        _logger.LogInformation($"Fetched {locations.Count} locations");

        var municipalities = await GetMunicipalities();
        _logger.LogInformation($"Fetched {municipalities.Count} municipalities");

        _logger.LogInformation("Building distances cache...");

        foreach (var target in locations)
        {
            var key = BuildKey(target.Name, target.County);
            var listOfSources = new List<CacheItem>();

            foreach (var source in municipalities)
            {
                // Don't include the target city itself
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

            // Sort by distance (closest first)
            listOfSources.Sort((first, second) => first.Distance.CompareTo(second.Distance));

            _cache.Set(key, listOfSources);
        }

        _logger.LogInformation("Locations cache successfully generated");
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

public class BlacklistCache
{
    private readonly ILogger<BlacklistCache> _logger;
    private readonly IMemoryCache _cache = new MemoryCache(new MemoryCacheOptions());

    public BlacklistCache(ILogger<BlacklistCache> logger)
    {
        _logger = logger;
    }

    public void BlacklistLocation(string city, string county, ServiceType serviceType)
    {
        var key = $"{serviceType}";

        if (_cache.Get(key) is not Dictionary<string, bool> blacklistByServiceType)
        {
            blacklistByServiceType = new Dictionary<string, bool>();
        }

        var locationKey = $"{city}::{county}";
        if (!blacklistByServiceType.ContainsKey(locationKey))
        {
            _logger.LogDebug($"Blacklisting {city}, {county} for {serviceType}");
            blacklistByServiceType[locationKey] = true;
            _cache.Set(key, blacklistByServiceType);
        }
    }

    public bool IsBlacklisted(ServiceType serviceTypeKey, string locationKey)
    {
        var key = $"{serviceTypeKey}";

        if (_cache.TryGetValue(key, out var value))
        {
            return value is Dictionary<string, bool> blacklistByServiceType
                && blacklistByServiceType.ContainsKey(locationKey);
        }

        return false;
    }
}

public class CacheItem
{
    public required string City { get; set; }
    public required string County { get; set; }
    public double Distance { get; set; }
}

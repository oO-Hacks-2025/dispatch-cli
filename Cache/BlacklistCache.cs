using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using testing.Models;

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

using EmergencyDispatcher.Domain.Models;
using Microsoft.Extensions.Caching.Memory;

namespace EmergencyDispatcher.Cache;

public class BlacklistCache(ILogger<BlacklistCache> logger)
{
    private readonly ILogger<BlacklistCache> _logger = logger;
    private readonly MemoryCache _cache = new(new MemoryCacheOptions());

    public void BlacklistLocation(string city, string county, ServiceType serviceType)
    {
        var key = $"{serviceType}";

        if (_cache.Get(key) is not Dictionary<string, bool> blacklistByServiceType)
        {
            blacklistByServiceType = [];
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

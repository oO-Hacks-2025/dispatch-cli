using Microsoft.Extensions.Caching.Memory;

namespace EmergencyDispatcher.Cache;

public class TokenCache
{
    private readonly MemoryCache _cache = new(new MemoryCacheOptions());

    public string GetToken()
    {
        if (_cache.TryGetValue("token", out string? token))
        {
            return token ?? string.Empty;
        }

        return string.Empty;
    }

    public void SetToken(string token)
    {
        _cache.Set("token", token);
    }

    public string GetRefreshToken()
    {
        if (_cache.TryGetValue("refreshToken", out string? refreshToken))
        {
            return refreshToken ?? string.Empty;
        }
        return string.Empty;
    }

    public void SetRefreshToken(string refreshToken)
    {
        _cache.Set("refreshToken", refreshToken);
    }

    public void ClearTokens()
    {
        _cache.Remove("token");
        _cache.Remove("refreshToken");
    }
}

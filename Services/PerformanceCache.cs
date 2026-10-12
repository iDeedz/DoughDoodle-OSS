using Microsoft.Extensions.Caching.Memory;

namespace DoughDoodle.Services;

public interface IPerformanceCache
{
    Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan? ttl = null);
}

public class PerformanceCache : IPerformanceCache
{
    private readonly IMemoryCache _cache;
    public PerformanceCache(IMemoryCache cache) => _cache = cache;

    public async Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan? ttl = null)
    {
        if (_cache.TryGetValue(key, out T? hit) && hit is not null) return hit;
        var value = await factory();
        _cache.Set(key, value, ttl ?? TimeSpan.FromMinutes(2));
        return value;
    }
}

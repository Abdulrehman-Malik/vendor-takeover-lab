using Microsoft.Extensions.Caching.Distributed;

namespace Orders.Api;

/// <summary>
/// Cache-aside wrapper around Redis. Redis is only a fast copy of the data, so if it is
/// down the API logs a warning and carries on reading from SQL Server.
/// </summary>
public class OrderCache(IDistributedCache cache, ILogger<OrderCache> log)
{
    static readonly DistributedCacheEntryOptions Ttl = new() { AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(60) };

    static string Key(int orderId) => $"order:{orderId}";

    public async Task<string?> GetAsync(int orderId)
    {
        try
        {
            var value = await cache.GetStringAsync(Key(orderId));
            if (value is null)
                log.LogInformation("Cache MISS {Key} - reading from SQL Server", Key(orderId));
            else
                log.LogInformation("Cache HIT {Key}", Key(orderId));
            return value;
        }
        catch (Exception ex)
        {
            log.LogWarning("Redis unavailable, skipping cache: {Message}", ex.Message);
            return null;
        }
    }

    public async Task SetAsync(int orderId, string json)
    {
        try { await cache.SetStringAsync(Key(orderId), json, Ttl); }
        catch (Exception ex) { log.LogWarning("Redis unavailable, not caching: {Message}", ex.Message); }
    }

    public async Task RemoveAsync(int orderId)
    {
        try { await cache.RemoveAsync(Key(orderId)); }
        catch (Exception ex) { log.LogWarning("Redis unavailable, could not evict: {Message}", ex.Message); }
    }
}

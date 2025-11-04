using StackExchange.Redis;

namespace DesiCorner.Gateway.Auth;

public sealed class RedisRateLimiter : IRedisRateLimiter
{
    private readonly IConnectionMultiplexer _mux;
    public RedisRateLimiter(IConnectionMultiplexer mux) => _mux = mux;

    public async Task<bool> ShouldLimitAsync(string bucketKey, int maxHits, TimeSpan window, CancellationToken ct)
    {
        var db = _mux.GetDatabase();
        var count = await db.StringIncrementAsync(bucketKey);
        if (count == 1) await db.KeyExpireAsync(bucketKey, window);
        return count > maxHits;
    }
}
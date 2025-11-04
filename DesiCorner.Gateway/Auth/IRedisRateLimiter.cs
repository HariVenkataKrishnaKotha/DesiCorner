namespace DesiCorner.Gateway.Auth;

public interface IRedisRateLimiter
{
    Task<bool> ShouldLimitAsync(string bucketKey, int maxHits, TimeSpan window, CancellationToken ct);
}
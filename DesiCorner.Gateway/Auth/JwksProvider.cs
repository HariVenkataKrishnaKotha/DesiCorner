using Microsoft.Extensions.Caching.Distributed;
using Microsoft.IdentityModel.Tokens;

namespace DesiCorner.Gateway.Auth;

public class JwksProvider : IJwksProvider
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _config;
    private readonly IDistributedCache _cache;
    private readonly ILogger<JwksProvider> _logger;
    private const string CacheKey = "jwks";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(1);

    public JwksProvider(
        HttpClient httpClient,
        IConfiguration config,
        IDistributedCache cache,
        ILogger<JwksProvider> logger)
    {
        _httpClient = httpClient;
        _config = config;
        _cache = cache;
        _logger = logger;
    }

    public async Task<JsonWebKeySet> GetAsync(CancellationToken ct)
    {
        var cached = await _cache.GetStringAsync(CacheKey, ct);
        if (!string.IsNullOrEmpty(cached))
        {
            _logger.LogDebug("JWKS retrieved from cache");
            return new JsonWebKeySet(cached);
        }

        var jwksUri = _config["Gateway:JwksUri"];
        if (string.IsNullOrEmpty(jwksUri))
        {
            throw new InvalidOperationException("Gateway:JwksUri not configured");
        }

        _logger.LogInformation("Fetching JWKS from {Uri}", jwksUri);

        var response = await _httpClient.GetAsync(jwksUri, ct);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(ct);

        var cacheOptions = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = CacheDuration
        };
        await _cache.SetStringAsync(CacheKey, json, cacheOptions, ct);

        _logger.LogInformation("JWKS cached for {Duration}", CacheDuration);

        return new JsonWebKeySet(json);
    }

    public async Task InvalidateAsync(CancellationToken ct)
    {
        await _cache.RemoveAsync(CacheKey, ct);
        _logger.LogInformation("JWKS cache invalidated");
    }
}
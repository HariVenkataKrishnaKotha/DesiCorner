using DesiCorner.Services.CartAPI.Models;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace DesiCorner.Services.CartAPI.Services;

public class RedisCartStore : ICartStore
{
    private readonly IDatabase _redisDb;

    public RedisCartStore(IConnectionMultiplexer redis)
    {
        _redisDb = redis.GetDatabase();
    }

    public async Task<Cart?> GetAsync(string key)
    {
        var cartJson = await _redisDb.StringGetAsync(key);
        return cartJson.IsNullOrEmpty ? null : JsonConvert.DeserializeObject<Cart>(cartJson!);
    }

    public async Task SaveAsync(string key, Cart cart, TimeSpan ttl)
    {
        var cartJson = JsonConvert.SerializeObject(cart);
        await _redisDb.StringSetAsync(key, cartJson, ttl);
    }

    public async Task DeleteAsync(string key)
    {
        await _redisDb.KeyDeleteAsync(key);
    }
}

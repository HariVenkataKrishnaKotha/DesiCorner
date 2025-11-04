using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;

namespace DesiCorner.Gateway.Auth;

public sealed class IntrospectionClient : IIntrospectionClient
{
    private readonly HttpClient _httpClient;
    private readonly IDistributedCache _cache;
    private readonly IConfiguration _cfg;
    private readonly ILogger<IntrospectionClient> _log;
    private static readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

    public IntrospectionClient(
        HttpClient httpClient,
        IDistributedCache cache,
        IConfiguration cfg,
        ILogger<IntrospectionClient> log)
    {
        _httpClient = httpClient;
        _cache = cache;
        _cfg = cfg;
        _log = log;
    }

    public async Task<(bool active, ClaimsPrincipal? principal, string? error)> IntrospectAsync(string token, CancellationToken ct)
    {
        var cacheKey = $"{_cfg["Redis:IntrospectionCachePrefix"]}{token}";
        var cached = await _cache.GetStringAsync(cacheKey, ct);
        if (!string.IsNullOrEmpty(cached))
        {
            var doc = JsonSerializer.Deserialize<IntrospectionDoc>(cached, _json);
            return (doc!.active, doc.active ? ToPrincipal(doc) : null, doc.active ? null : "inactive_cached");
        }

        var endpoint = _cfg["Gateway:Introspection:Endpoint"]!;
        var clientId = _cfg["Gateway:Introspection:ClientId"]!;
        var clientSecret = _cfg["Gateway:Introspection:ClientSecret"]!;

        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Basic",
                Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}")));

        using var content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string,string>("token", token)
        });

        using var resp = await _httpClient.PostAsync(endpoint, content, ct);
        if (!resp.IsSuccessStatusCode)
        {
            _log.LogWarning("Introspection HTTP {Status}", resp.StatusCode);
            return (false, null, "http_failure");
        }

        var json = await resp.Content.ReadAsStringAsync(ct);
        var result = JsonSerializer.Deserialize<IntrospectionDoc>(json, _json) ?? new IntrospectionDoc();

        var ttl = result.exp.HasValue
            ? TimeSpan.FromSeconds(Math.Max(0, result.exp.Value - DateTimeOffset.UtcNow.ToUnixTimeSeconds()))
            : TimeSpan.FromMinutes(5);

        await _cache.SetStringAsync(cacheKey, json, new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = ttl
        }, ct);

        return result.active ? (true, ToPrincipal(result), null) : (false, null, "inactive");
    }

    private static ClaimsPrincipal ToPrincipal(IntrospectionDoc d)
    {
        var claims = new List<Claim>();
        void Add(string type, string? value)
        {
            if (!string.IsNullOrEmpty(value)) claims.Add(new Claim(type, value));
        }

        Add("sub", d.sub);
        Add("client_id", d.client_id);
        Add("username", d.username);

        if (d.scope is not null)
        {
            foreach (var s in d.scope.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                claims.Add(new Claim("scope", s));
        }
        if (d.role is not null)
        {
            foreach (var r in d.role) claims.Add(new Claim(ClaimTypes.Role, r));
        }

        if (d.permission is not null)
        {
            foreach (var p in d.permission) claims.Add(new Claim("permission", p));
        }

        return new ClaimsPrincipal(new ClaimsIdentity(claims, "introspection"));
    }

    private sealed class IntrospectionDoc
    {
        public bool active { get; set; }
        public string? sub { get; set; }
        public string? username { get; set; }
        public string? client_id { get; set; }
        public string? scope { get; set; }
        public string[]? role { get; set; }
        public string[]? permission { get; set; }
        public long? exp { get; set; }
    }
}
using System.Text.Json.Serialization;

namespace DesiCorner.Services.OrderAPI.Services;

/// <summary>
/// Obtains OrderAPI's own access token from AuthServer with the client-credentials grant and
/// caches it until shortly before it expires. Returns null when no token can be had; callers
/// then proceed without one.
/// </summary>
public interface IServiceTokenProvider
{
    /// <summary>False when no client secret is configured: no token can ever be obtained.</summary>
    bool IsConfigured { get; }

    Task<string?> GetTokenAsync(CancellationToken ct);
}

public sealed class ServiceTokenProvider : IServiceTokenProvider
{
    public const string HttpClientName = "AuthServerToken";
    public const string ClientIdKey = "ServiceAuth:ClientId";
    public const string ClientSecretKey = "ServiceAuth:ClientSecret";
    public const string DefaultClientId = "desicorner-order-service";
    public const string UserLookupScope = "desicorner.internal.user-lookup";

    // Renew this long before expiry so a token never lapses in flight
    private static readonly TimeSpan RefreshSkew = TimeSpan.FromSeconds(60);

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly TimeProvider _time;
    private readonly ILogger<ServiceTokenProvider> _logger;
    private readonly SemaphoreSlim _lock = new(1, 1);

    private string? _token;
    private DateTimeOffset _renewAt;

    public ServiceTokenProvider(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        TimeProvider time,
        ILogger<ServiceTokenProvider> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _time = time;
        _logger = logger;
    }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_configuration[ClientSecretKey]);

    public async Task<string?> GetTokenAsync(CancellationToken ct)
    {
        var secret = _configuration[ClientSecretKey];
        if (string.IsNullOrWhiteSpace(secret))
        {
            return null;
        }

        if (_token != null && _time.GetUtcNow() < _renewAt)
        {
            return _token;
        }

        await _lock.WaitAsync(ct);
        try
        {
            if (_token != null && _time.GetUtcNow() < _renewAt)
            {
                return _token;
            }

            var client = _httpClientFactory.CreateClient(HttpClientName);
            using var response = await client.PostAsync("/connect/token", new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials",
                ["client_id"] = _configuration[ClientIdKey] ?? DefaultClientId,
                ["client_secret"] = secret,
                ["scope"] = UserLookupScope
            }), ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Service token request failed with {Status}", response.StatusCode);
                return null;
            }

            var token = await response.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken: ct);
            if (string.IsNullOrEmpty(token?.AccessToken))
            {
                _logger.LogWarning("Service token response carried no access token");
                return null;
            }

            _token = token.AccessToken;
            _renewAt = _time.GetUtcNow() + TimeSpan.FromSeconds(token.ExpiresIn) - RefreshSkew;
            return _token;
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !ct.IsCancellationRequested)
        {
            _logger.LogWarning(ex, "Could not obtain a service token from AuthServer");
            return null;
        }
        finally
        {
            _lock.Release();
        }
    }

    private sealed record TokenResponse(
        [property: JsonPropertyName("access_token")] string? AccessToken,
        [property: JsonPropertyName("expires_in")] int ExpiresIn);
}

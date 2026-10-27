using System.Text.Json;
using DesiCorner.Contracts.Cart;
using DesiCorner.Contracts.Common;

namespace DesiCorner.Services.PaymentAPI.Services;

/// <summary>
/// Reads the caller's cart through the gateway, passing along the caller's own
/// credentials (bearer token or guest X-Session-Id). PaymentAPI never names a user itself;
/// the gateway and CartAPI decide whose cart it is.
/// </summary>
public class CartClient : ICartClient
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<CartClient> _logger;

    public CartClient(
        IHttpClientFactory httpClientFactory,
        IHttpContextAccessor httpContextAccessor,
        ILogger<CartClient> logger)
    {
        _httpClientFactory = httpClientFactory;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task<CartDto?> GetCallerCartAsync(CancellationToken ct = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/cart");

        var incoming = _httpContextAccessor.HttpContext?.Request;
        if (incoming != null)
        {
            var authorization = incoming.Headers.Authorization.ToString();
            if (!string.IsNullOrEmpty(authorization))
            {
                request.Headers.TryAddWithoutValidation("Authorization", authorization);
            }

            var sessionId = incoming.Headers["X-Session-Id"].ToString();
            if (!string.IsNullOrEmpty(sessionId))
            {
                request.Headers.TryAddWithoutValidation("X-Session-Id", sessionId);
            }
        }

        var client = _httpClientFactory.CreateClient("CartAPI");
        var response = await client.SendAsync(request, ct);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Cart lookup failed with status {StatusCode}", response.StatusCode);
            return null;
        }

        var body = await response.Content.ReadFromJsonAsync<ResponseDto>(cancellationToken: ct);
        if (body?.IsSuccess != true || body.Result == null)
        {
            return null;
        }

        var cartJson = JsonSerializer.Serialize(body.Result);
        return JsonSerializer.Deserialize<CartDto>(cartJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }
}

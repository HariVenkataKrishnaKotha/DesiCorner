using System.Net.Http.Headers;

namespace DesiCorner.Services.OrderAPI.Services;

/// <summary>
/// Attaches OrderAPI's client-credentials token to calls on the "AuthAPI" client (the
/// service-only user lookup). Without a token the request goes out unauthenticated, AuthServer
/// refuses it, and the caller falls back as before (a guest order stays a guest order).
/// </summary>
public class ServiceTokenHandler : DelegatingHandler
{
    private readonly IServiceTokenProvider _tokens;

    public ServiceTokenHandler(IServiceTokenProvider tokens)
    {
        _tokens = tokens;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.Headers.Authorization == null &&
            await _tokens.GetTokenAsync(cancellationToken) is { } token)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return await base.SendAsync(request, cancellationToken);
    }
}

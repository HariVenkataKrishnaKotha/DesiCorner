using System.Net;
using System.Net.Http.Headers;

namespace DesiCorner.Services.OrderAPI.Services;

/// <summary>
/// Attaches OrderAPI's client-credentials token to calls on the "AuthAPI" client (the
/// service-only user lookup). With no client secret configured (allowed only in Development)
/// the call is not sent: it is answered 401 locally. If a token just can't be obtained, the
/// request goes out unauthenticated and AuthServer refuses it. Either way the caller falls back
/// as before (a guest order stays a guest order).
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
        if (request.Headers.Authorization == null && !_tokens.IsConfigured)
        {
            return new HttpResponseMessage(HttpStatusCode.Unauthorized)
            {
                RequestMessage = request,
                ReasonPhrase = "OrderAPI has no service client secret configured"
            };
        }

        if (request.Headers.Authorization == null &&
            await _tokens.GetTokenAsync(cancellationToken) is { } token)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return await base.SendAsync(request, cancellationToken);
    }
}

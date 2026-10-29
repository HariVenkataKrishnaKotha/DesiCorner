using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace DesiCorner.Tests.Shared;

/// <summary>
/// Reads the TLS-related settings a hosted service actually ended up with: whether a named
/// HttpClient's primary handler accepts any server certificate, and whether JwtBearer insists
/// on HTTPS metadata.
/// </summary>
public static class TlsInspection
{
    public static bool AcceptsAnyServerCertificate(IServiceProvider services, string clientName)
    {
        var handler = services.GetRequiredService<IHttpMessageHandlerFactory>().CreateHandler(clientName);
        while (handler is DelegatingHandler delegating)
        {
            handler = delegating.InnerHandler!;
        }

        return handler switch
        {
            HttpClientHandler h => h.ServerCertificateCustomValidationCallback != null,
            SocketsHttpHandler s => s.SslOptions.RemoteCertificateValidationCallback != null,
            _ => throw new InvalidOperationException($"Unexpected primary handler {handler.GetType()} for {clientName}")
        };
    }

    public static bool RequiresHttpsMetadata(IServiceProvider services, string scheme = JwtBearerDefaults.AuthenticationScheme) =>
        services.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>().Get(scheme).RequireHttpsMetadata;
}

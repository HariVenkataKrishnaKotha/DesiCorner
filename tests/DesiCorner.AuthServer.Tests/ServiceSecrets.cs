using System.Security.Cryptography;

namespace DesiCorner.AuthServer.Tests;

/// <summary>Configuration keys for the confidential clients' secrets, with values generated per test run.</summary>
public static class ServiceSecrets
{
    public const string OrderServiceKey = "OpenIddict:Clients:OrderService:ClientSecret";
    public const string GatewayKey = "OpenIddict:Clients:Gateway:ClientSecret";

    public const string OrderServiceClientId = "desicorner-order-service";
    public const string UserLookupScope = "desicorner.internal.user-lookup";

    public static string New() => Convert.ToHexString(RandomNumberGenerator.GetBytes(24));

    public static Dictionary<string, string?> Both(out string orderServiceSecret)
    {
        orderServiceSecret = New();
        return new Dictionary<string, string?>
        {
            [OrderServiceKey] = orderServiceSecret,
            [GatewayKey] = New()
        };
    }
}

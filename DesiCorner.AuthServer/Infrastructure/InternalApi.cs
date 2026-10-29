namespace DesiCorner.AuthServer.Infrastructure;

/// <summary>
/// Service-to-service access to AuthServer's own API. Only confidential service clients can
/// obtain these scopes (client-credentials grant), so no user token ever carries them.
/// </summary>
public static class InternalApi
{
    /// <summary>OrderAPI's confidential client (client-credentials grant only).</summary>
    public const string OrderServiceClientId = "desicorner-order-service";

    /// <summary>Lets the holder ask whether an email or phone number has an account.</summary>
    public const string UserLookupScope = "desicorner.internal.user-lookup";

    /// <summary>Authorization policy requiring <see cref="UserLookupScope"/>.</summary>
    public const string UserLookupPolicy = "UserLookupService";

    /// <summary>Configuration key for the order service client's secret (user-secrets or environment).</summary>
    public const string OrderServiceSecretKey = "OpenIddict:Clients:OrderService:ClientSecret";
}

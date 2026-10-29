namespace DesiCorner.AuthServer.Infrastructure;

/// <summary>
/// Secrets of the confidential OpenIddict clients. They are read only from configuration
/// (user-secrets in development, environment variables elsewhere), never from committed files.
/// </summary>
public static class ClientSecrets
{
    public const string OrderServiceKey = InternalApi.OrderServiceSecretKey;
    public const string GatewayKey = "OpenIddict:Clients:Gateway:ClientSecret";

    private static readonly string[] Required = { OrderServiceKey, GatewayKey };

    /// <summary>
    /// Outside Development, stop startup when a secret is missing. In Development the seeding
    /// skips the affected client and logs a warning instead.
    /// </summary>
    public static void EnsureConfigured(IConfiguration configuration, IHostEnvironment environment)
    {
        if (environment.IsDevelopment())
        {
            return;
        }

        var missing = Required.Where(key => string.IsNullOrWhiteSpace(configuration[key])).ToList();
        if (missing.Count > 0)
        {
            throw new InvalidOperationException(
                "Missing client secret configuration: " + string.Join(", ", missing.Select(key =>
                    $"{key} (set the {key.Replace(":", "__")} environment variable)")));
        }
    }
}

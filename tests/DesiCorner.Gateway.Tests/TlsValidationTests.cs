using DesiCorner.Tests.Shared;

namespace DesiCorner.Gateway.Tests;

/// <summary>
/// The gateway's JWKS and introspection clients accepted any server certificate in every
/// environment. Only Development (local dev certificates) may skip validation now.
/// </summary>
public class TlsValidationTests
{
    // Typed clients are named after their service type
    private static readonly string[] Clients = { "IJwksProvider", "IIntrospectionClient" };

    [Theory]
    [InlineData("Production")]
    [InlineData("Testing")]
    public void Outside_Development_server_certificates_are_validated(string environment)
    {
        using var factory = new GatewayFactory { EnvironmentName = environment, StubIntrospection = false };
        factory.CreateClient();

        Assert.All(Clients, name => Assert.False(TlsInspection.AcceptsAnyServerCertificate(factory.Services, name), name));
    }

    [Fact]
    public void Development_still_accepts_local_dev_certificates()
    {
        using var factory = new GatewayFactory { EnvironmentName = "Development", StubIntrospection = false };
        factory.CreateClient();

        Assert.All(Clients, name => Assert.True(TlsInspection.AcceptsAnyServerCertificate(factory.Services, name), name));
    }
}

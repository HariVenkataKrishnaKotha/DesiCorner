using DesiCorner.Tests.Shared;

namespace DesiCorner.Services.OrderAPI.Tests;

/// <summary>
/// The PaymentAPI client accepted any server certificate and JwtBearer allowed HTTP metadata in
/// every environment. Both relaxations are now Development-only.
/// </summary>
public class TlsValidationTests
{
    [Theory]
    [InlineData("Production")]
    [InlineData("Testing")]
    public void Outside_Development_certificates_and_https_metadata_are_enforced(string environment)
    {
        using var factory = new OrderApiFactory { EnvironmentName = environment, StubDownstream = false };
        factory.CreateClient();

        Assert.False(TlsInspection.AcceptsAnyServerCertificate(factory.Services, "PaymentAPI"));
        Assert.True(TlsInspection.RequiresHttpsMetadata(factory.Services));
    }

    [Fact]
    public void Development_keeps_the_local_relaxations()
    {
        using var factory = new OrderApiFactory { EnvironmentName = "Development", StubDownstream = false };
        factory.CreateClient();

        Assert.True(TlsInspection.AcceptsAnyServerCertificate(factory.Services, "PaymentAPI"));
        Assert.False(TlsInspection.RequiresHttpsMetadata(factory.Services));
    }
}

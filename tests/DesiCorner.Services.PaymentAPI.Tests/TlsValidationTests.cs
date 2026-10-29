using DesiCorner.Tests.Shared;

namespace DesiCorner.Services.PaymentAPI.Tests;

/// <summary>
/// The OrderAPI client accepted any server certificate in every environment; only Development
/// may skip validation now.
/// </summary>
public class TlsValidationTests
{
    [Theory]
    [InlineData("Production")]
    [InlineData("Testing")]
    public void Outside_Development_server_certificates_are_validated(string environment)
    {
        using var factory = new PaymentApiFactory(webhookSecret: "test-webhook-secret", environment);
        factory.CreateClient();

        Assert.False(TlsInspection.AcceptsAnyServerCertificate(factory.Services, "OrderAPI"));
    }

    [Fact]
    public void Development_still_accepts_local_dev_certificates()
    {
        using var factory = new PaymentApiFactory(webhookSecret: null, "Development");
        factory.CreateClient();

        Assert.True(TlsInspection.AcceptsAnyServerCertificate(factory.Services, "OrderAPI"));
    }
}

using DesiCorner.Tests.Shared;

namespace DesiCorner.Services.CartAPI.Tests;

/// <summary>JwtBearer allowed HTTP metadata in every environment; only Development may now.</summary>
public class TlsValidationTests : IClassFixture<CartApiFactory>
{
    private readonly CartApiFactory _factory;

    public TlsValidationTests(CartApiFactory factory) => _factory = factory;

    [Fact]
    public void Outside_Development_https_metadata_is_required()
    {
        _factory.CreateClient();

        Assert.True(TlsInspection.RequiresHttpsMetadata(_factory.Services));
    }
}

namespace DesiCorner.Services.CartAPI.Services;

/// <summary>ProductAPI could not be asked about a product, so an operation that depends on it stopped.</summary>
public class ProductCatalogUnavailableException : Exception
{
    public ProductCatalogUnavailableException(string message) : base(message)
    {
    }
}

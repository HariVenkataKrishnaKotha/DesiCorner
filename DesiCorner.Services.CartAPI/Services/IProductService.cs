namespace DesiCorner.Services.CartAPI.Services;

public interface IProductService
{
    /// <summary>The product, or null if it doesn't exist or ProductAPI couldn't be reached.</summary>
    Task<ProductDetails?> GetProductAsync(Guid productId, CancellationToken ct = default);

    /// <summary>
    /// Like <see cref="GetProductAsync"/>, but tells "the product doesn't exist" apart from
    /// "ProductAPI couldn't be asked", for callers that must not treat an outage as a deletion.
    /// </summary>
    Task<ProductLookup> LookupProductAsync(Guid productId, CancellationToken ct = default);
}

public enum ProductLookupStatus
{
    Found,
    NotFound,
    Failed
}

public sealed record ProductLookup(ProductLookupStatus Status, ProductDetails? Product = null)
{
    public static readonly ProductLookup NotFound = new(ProductLookupStatus.NotFound);
    public static readonly ProductLookup Failed = new(ProductLookupStatus.Failed);
}

public class ProductDetails
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string? ImageUrl { get; set; }
    public bool IsAvailable { get; set; }
}
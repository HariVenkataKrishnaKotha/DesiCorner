namespace DesiCorner.Services.CartAPI.Services;

public interface IProductService
{
    Task<ProductDetails?> GetProductAsync(Guid productId, CancellationToken ct = default);
}

public class ProductDetails
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string? ImageUrl { get; set; }
    public bool IsAvailable { get; set; }
}
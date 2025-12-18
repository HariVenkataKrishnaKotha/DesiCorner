using DesiCorner.Contracts.Products;

namespace DesiCorner.Services.ProductAPI.Services;

public interface IProductService
{
    Task<List<ProductDto>> GetAllProductsAsync(CancellationToken ct = default);
    Task<List<ProductDto>> GetProductsByCategoryAsync(Guid categoryId, CancellationToken ct = default);
    Task<ProductDto?> GetProductByIdAsync(Guid id, CancellationToken ct = default);
    Task<ProductDto> CreateProductAsync(CreateProductDto dto, CancellationToken ct = default);
    Task<ProductDto?> UpdateProductAsync(UpdateProductDto dto, CancellationToken ct = default);
    Task<bool> DeleteProductAsync(Guid id, CancellationToken ct = default);
    Task<ProductStatsDto> GetProductStatsAsync(CancellationToken ct = default);
}
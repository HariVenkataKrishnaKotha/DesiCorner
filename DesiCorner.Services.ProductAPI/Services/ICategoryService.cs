using DesiCorner.Contracts.Products;

namespace DesiCorner.Services.ProductAPI.Services;

public interface ICategoryService
{
    Task<List<CategoryDto>> GetAllCategoriesAsync(CancellationToken ct = default);
    Task<CategoryDto?> GetCategoryByIdAsync(Guid id, CancellationToken ct = default);
    Task<CategoryDto> CreateCategoryAsync(CategoryDto dto, CancellationToken ct = default);
    Task<CategoryDto?> UpdateCategoryAsync(CategoryDto dto, CancellationToken ct = default);
    Task<bool> DeleteCategoryAsync(Guid id, CancellationToken ct = default);
}
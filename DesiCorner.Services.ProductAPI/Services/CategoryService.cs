using DesiCorner.Contracts.Products;
using DesiCorner.MessageBus.Redis;
using DesiCorner.Services.ProductAPI.Data;
using DesiCorner.Services.ProductAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace DesiCorner.Services.ProductAPI.Services;

public class CategoryService : ICategoryService
{
    private readonly ProductDbContext _db;
    private readonly ICacheService _cache;
    private readonly ILogger<CategoryService> _logger;

    public CategoryService(
        ProductDbContext db,
        ICacheService cache,
        ILogger<CategoryService> logger)
    {
        _db = db;
        _cache = cache;
        _logger = logger;
    }

    public async Task<List<CategoryDto>> GetAllCategoriesAsync(CancellationToken ct = default)
    {
        var cacheKey = RedisKeys.CategoryList();
        var cached = await _cache.GetAsync<List<CategoryDto>>(cacheKey, ct);
        if (cached != null)
        {
            _logger.LogDebug("Categories retrieved from cache");
            return cached;
        }

        var categories = await _db.Categories
            .OrderBy(c => c.DisplayOrder)
            .ToListAsync(ct);

        var result = categories.Select(MapToDto).ToList();

        await _cache.SetAsync(cacheKey, result, TimeSpan.FromMinutes(30), ct);

        return result;
    }

    public async Task<CategoryDto?> GetCategoryByIdAsync(Guid id, CancellationToken ct = default)
    {
        var cacheKey = RedisKeys.Category(id);
        var cached = await _cache.GetAsync<CategoryDto>(cacheKey, ct);
        if (cached != null)
        {
            return cached;
        }

        var category = await _db.Categories.FindAsync(new object[] { id }, ct);
        if (category == null)
            return null;

        var result = MapToDto(category);

        await _cache.SetAsync(cacheKey, result, TimeSpan.FromMinutes(30), ct);

        return result;
    }

    public async Task<CategoryDto> CreateCategoryAsync(CategoryDto dto, CancellationToken ct = default)
    {
        var category = new Category
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
            Description = dto.Description,
            ImageUrl = dto.ImageUrl,
            DisplayOrder = dto.DisplayOrder,
            CreatedAt = DateTime.UtcNow
        };

        _db.Categories.Add(category);
        await _db.SaveChangesAsync(ct);

        await _cache.RemoveAsync(RedisKeys.CategoryList(), ct);

        return MapToDto(category);
    }

    public async Task<CategoryDto?> UpdateCategoryAsync(CategoryDto dto, CancellationToken ct = default)
    {
        var category = await _db.Categories.FindAsync(new object[] { dto.Id }, ct);
        if (category == null)
            return null;

        category.Name = dto.Name;
        category.Description = dto.Description;
        category.ImageUrl = dto.ImageUrl;
        category.DisplayOrder = dto.DisplayOrder;

        await _db.SaveChangesAsync(ct);

        await _cache.RemoveAsync(RedisKeys.Category(dto.Id), ct);
        await _cache.RemoveAsync(RedisKeys.CategoryList(), ct);

        return MapToDto(category);
    }

    public async Task<bool> DeleteCategoryAsync(Guid id, CancellationToken ct = default)
    {
        var category = await _db.Categories.FindAsync(new object[] { id }, ct);
        if (category == null)
            return false;

        // Check if category has products
        var hasProducts = await _db.Products.AnyAsync(p => p.CategoryId == id, ct);
        if (hasProducts)
        {
            throw new InvalidOperationException("Cannot delete category with existing products");
        }

        _db.Categories.Remove(category);
        await _db.SaveChangesAsync(ct);

        await _cache.RemoveAsync(RedisKeys.Category(id), ct);
        await _cache.RemoveAsync(RedisKeys.CategoryList(), ct);

        return true;
    }

    private static CategoryDto MapToDto(Category category)
    {
        return new CategoryDto
        {
            Id = category.Id,
            Name = category.Name,
            Description = category.Description,
            ImageUrl = category.ImageUrl,
            DisplayOrder = category.DisplayOrder
        };
    }
}
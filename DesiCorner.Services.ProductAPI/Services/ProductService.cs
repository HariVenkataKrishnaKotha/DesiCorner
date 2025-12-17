using DesiCorner.Contracts.Products;
using DesiCorner.MessageBus.Redis;
using DesiCorner.Services.ProductAPI.Data;
using DesiCorner.Services.ProductAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace DesiCorner.Services.ProductAPI.Services;

public class ProductService : IProductService
{
    private readonly ProductDbContext _db;
    private readonly ICacheService _cache;
    private readonly ILogger<ProductService> _logger;

    public ProductService(
        ProductDbContext db,
        ICacheService cache,
        ILogger<ProductService> logger)
    {
        _db = db;
        _cache = cache;
        _logger = logger;
    }

    public async Task<List<ProductDto>> GetAllProductsAsync(CancellationToken ct = default)
    {
        // Try cache first
        var cacheKey = RedisKeys.ProductList("all");
        var cached = await _cache.GetAsync<List<ProductDto>>(cacheKey, ct);
        if (cached != null)
        {
            _logger.LogDebug("Products retrieved from cache");
            return cached;
        }

        // Get from database
        var products = await _db.Products
            .Include(p => p.Category)
            .OrderBy(p => p.Name)
            .ToListAsync(ct);

        var result = products.Select(MapToDto).ToList();

        // Cache for 5 minutes
        await _cache.SetAsync(cacheKey, result, TimeSpan.FromMinutes(5), ct);

        return result;
    }

    public async Task<List<ProductDto>> GetProductsByCategoryAsync(Guid categoryId, CancellationToken ct = default)
    {
        var cacheKey = RedisKeys.ProductList($"category:{categoryId}");
        var cached = await _cache.GetAsync<List<ProductDto>>(cacheKey, ct);
        if (cached != null)
        {
            return cached;
        }

        var products = await _db.Products
            .Include(p => p.Category)
            .Where(p => p.CategoryId == categoryId)
            .OrderBy(p => p.Name)
            .ToListAsync(ct);

        var result = products.Select(MapToDto).ToList();

        await _cache.SetAsync(cacheKey, result, TimeSpan.FromMinutes(5), ct);

        return result;
    }

    public async Task<ProductDto?> GetProductByIdAsync(Guid id, CancellationToken ct = default)
    {
        var cacheKey = RedisKeys.Product(id);
        var cached = await _cache.GetAsync<ProductDto>(cacheKey, ct);
        if (cached != null)
        {
            return cached;
        }

        var product = await _db.Products
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

        if (product == null)
            return null;

        var result = MapToDto(product);

        await _cache.SetAsync(cacheKey, result, TimeSpan.FromMinutes(10), ct);

        return result;
    }

    public async Task<ProductDto> CreateProductAsync(CreateProductDto dto, CancellationToken ct = default)
    {
        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
            Description = dto.Description,
            Price = dto.Price,
            CategoryId = dto.CategoryId,
            ImageUrl = dto.ImageUrl,
            IsAvailable = dto.IsAvailable,
            IsVegetarian = dto.IsVegetarian,
            IsVegan = dto.IsVegan,
            IsSpicy = dto.IsSpicy,
            SpiceLevel = dto.SpiceLevel,
            Allergens = dto.Allergens,
            PreparationTime = dto.PreparationTime,
            CreatedAt = DateTime.UtcNow
        };

        _db.Products.Add(product);
        await _db.SaveChangesAsync(ct);

        // Invalidate cache
        await _cache.RemoveByPrefixAsync("products:", ct);

        // Load category for DTO
        await _db.Entry(product).Reference(p => p.Category).LoadAsync(ct);

        return MapToDto(product);
    }

    public async Task<ProductDto?> UpdateProductAsync(UpdateProductDto dto, CancellationToken ct = default)
    {
        var product = await _db.Products.FindAsync(new object[] { dto.Id }, ct);
        if (product == null)
            return null;

        product.Name = dto.Name;
        product.Description = dto.Description;
        product.Price = dto.Price;
        product.CategoryId = dto.CategoryId;
        product.ImageUrl = dto.ImageUrl;
        product.IsAvailable = dto.IsAvailable;
        product.IsVegetarian = dto.IsVegetarian;
        product.IsVegan = dto.IsVegan;
        product.IsSpicy = dto.IsSpicy;
        product.SpiceLevel = dto.SpiceLevel;
        product.Allergens = dto.Allergens;
        product.PreparationTime = dto.PreparationTime;
        product.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        // Invalidate cache
        await _cache.RemoveAsync(RedisKeys.Product(dto.Id), ct);
        await _cache.RemoveByPrefixAsync("products:", ct);

        await _db.Entry(product).Reference(p => p.Category).LoadAsync(ct);

        return MapToDto(product);
    }

    public async Task<bool> DeleteProductAsync(Guid id, CancellationToken ct = default)
    {
        var product = await _db.Products.FindAsync(new object[] { id }, ct);
        if (product == null)
            return false;

        _db.Products.Remove(product);
        await _db.SaveChangesAsync(ct);

        // Invalidate cache
        await _cache.RemoveAsync(RedisKeys.Product(id), ct);
        await _cache.RemoveByPrefixAsync("products:", ct);

        return true;
    }

    private static ProductDto MapToDto(Product product)
    {
        return new ProductDto
        {
            Id = product.Id,
            Name = product.Name,
            Description = product.Description,
            Price = product.Price,
            ImageUrl = product.ImageUrl,
            CategoryId = product.CategoryId,
            CategoryName = product.Category?.Name ?? "",
            IsAvailable = product.IsAvailable,
            IsVegetarian = product.IsVegetarian,
            IsVegan = product.IsVegan,
            IsSpicy = product.IsSpicy,
            SpiceLevel = product.SpiceLevel,
            Allergens = product.Allergens,
            PreparationTime = product.PreparationTime,
            CreatedAt = product.CreatedAt,
            UpdatedAt = product.UpdatedAt,
            // Rating aggregation
            AverageRating = product.AverageRating,
            ReviewCount = product.ReviewCount
        };
    }
}
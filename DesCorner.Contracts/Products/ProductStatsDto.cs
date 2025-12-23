namespace DesiCorner.Contracts.Products;

public class ProductStatsDto
{
    public int TotalProducts { get; set; }
    public int ActiveProducts { get; set; }
    public int OutOfStockProducts { get; set; }
    public int LowStockProducts { get; set; } // Stock < 10
    public int FeaturedProducts { get; set; }
    public decimal AveragePrice { get; set; }
    public decimal AverageRating { get; set; }
    public int TotalReviews { get; set; }
    public List<CategoryStatsDto> CategoryBreakdown { get; set; } = new();
    public List<TopProductDto> TopRatedProducts { get; set; } = new();
}

public class CategoryStatsDto
{
    public Guid CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public int ProductCount { get; set; }
    public decimal AveragePrice { get; set; }
}

public class TopProductDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public decimal Price { get; set; }
    public double AverageRating { get; set; }
    public int ReviewCount { get; set; }
}
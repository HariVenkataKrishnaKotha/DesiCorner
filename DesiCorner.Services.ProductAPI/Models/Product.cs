namespace DesiCorner.Services.ProductAPI.Models;

public class Product
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string? ImageUrl { get; set; }
    public Guid CategoryId { get; set; }
    public bool IsAvailable { get; set; } = true;
    public bool IsVegetarian { get; set; }
    public bool IsVegan { get; set; }
    public bool IsSpicy { get; set; }
    public int SpiceLevel { get; set; }
    public string? Allergens { get; set; }
    public int PreparationTime { get; set; } = 15;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Rating aggregation (updated when reviews change)
    public double AverageRating { get; set; } = 0;
    public int ReviewCount { get; set; } = 0;

    // Navigation
    public Category Category { get; set; } = null!;
    public ICollection<Review> Reviews { get; set; } = new List<Review>();
}
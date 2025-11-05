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

    // Navigation
    public Category Category { get; set; } = null!;
}
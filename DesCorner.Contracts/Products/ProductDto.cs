using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DesiCorner.Contracts.Products;

public class ProductDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string? ImageUrl { get; set; }
    public Guid CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public bool IsAvailable { get; set; }
    public bool IsVegetarian { get; set; }
    public bool IsVegan { get; set; }
    public bool IsSpicy { get; set; }
    public int SpiceLevel { get; set; } // 0-5
    public string? Allergens { get; set; } // Comma-separated
    public int PreparationTime { get; set; } // In minutes
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

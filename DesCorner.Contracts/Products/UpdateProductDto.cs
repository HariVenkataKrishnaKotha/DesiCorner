using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace DesiCorner.Contracts.Products;

public class UpdateProductDto
{
    [Required]
    public Guid Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(500)]
    public string Description { get; set; } = string.Empty;

    [Required]
    [Range(0.01, 10000)]
    public decimal Price { get; set; }

    [Required]
    public Guid CategoryId { get; set; }

    public string? ImageUrl { get; set; }

    public bool IsAvailable { get; set; }

    public bool IsVegetarian { get; set; }

    public bool IsVegan { get; set; }

    public bool IsSpicy { get; set; }

    [Range(0, 5)]
    public int SpiceLevel { get; set; }

    public string? Allergens { get; set; }

    [Range(5, 120)]
    public int PreparationTime { get; set; }
}

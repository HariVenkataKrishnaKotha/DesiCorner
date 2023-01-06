using System.ComponentModel;

namespace DesiCorner.Models
{
    public class Dish
    {
        public int DishId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? ShortDescription { get; set; }
        public string? LongDescription { get; set; }
        public string? AllergicInformation { get; set; }
        public decimal Price { get; set; }
        public string? ImageURL { get; set; }
        public string? ImageThumbnailUrl { get; set; }
        public bool IsDishofTheWeek { get; set; }
        public bool InStock { get; set; }
        public int CategoryID { get; set; }
        public Category Category { get; set; } = default!;
    }
}

using DesiCorner.Services.ProductAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace DesiCorner.Services.ProductAPI.Data;

public class ProductDbContext : DbContext
{
    public ProductDbContext(DbContextOptions<ProductDbContext> options) : base(options) { }

    public DbSet<Product> Products { get; set; }
    public DbSet<Category> Categories { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Category configuration
        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.ImageUrl).HasMaxLength(500);
            entity.HasIndex(e => e.Name).IsUnique();
        });

        // Product configuration
        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.Price).HasColumnType("decimal(18,2)");
            entity.Property(e => e.ImageUrl).HasMaxLength(500);
            entity.Property(e => e.Allergens).HasMaxLength(200);

            entity.HasOne(e => e.Category)
                .WithMany(c => c.Products)
                .HasForeignKey(e => e.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => e.Name);
            entity.HasIndex(e => e.CategoryId);
        });

        // Seed data
        SeedData(modelBuilder);
    }

    private void SeedData(ModelBuilder modelBuilder)
    {
        // Categories
        var categoryAppetizers = new Category
        {
            Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Name = "Appetizers",
            Description = "Start your meal with these delicious appetizers",
            DisplayOrder = 1,
            CreatedAt = DateTime.UtcNow
        };

        var categoryMainCourse = new Category
        {
            Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
            Name = "Main Course",
            Description = "Hearty main dishes to satisfy your hunger",
            DisplayOrder = 2,
            CreatedAt = DateTime.UtcNow
        };

        var categoryBiryani = new Category
        {
            Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
            Name = "Biryani",
            Description = "Aromatic rice dishes with your choice of protein",
            DisplayOrder = 3,
            CreatedAt = DateTime.UtcNow
        };

        var categoryDesserts = new Category
        {
            Id = Guid.Parse("44444444-4444-4444-4444-444444444444"),
            Name = "Desserts",
            Description = "Sweet endings to your perfect meal",
            DisplayOrder = 4,
            CreatedAt = DateTime.UtcNow
        };

        var categoryBeverages = new Category
        {
            Id = Guid.Parse("55555555-5555-5555-5555-555555555555"),
            Name = "Beverages",
            Description = "Refreshing drinks to complement your meal",
            DisplayOrder = 5,
            CreatedAt = DateTime.UtcNow
        };

        modelBuilder.Entity<Category>().HasData(
            categoryAppetizers, categoryMainCourse, categoryBiryani, categoryDesserts, categoryBeverages
        );

        // Products
        modelBuilder.Entity<Product>().HasData(
            new Product
            {
                Id = Guid.NewGuid(),
                Name = "Samosa (2 pcs)",
                Description = "Crispy pastry filled with spiced potatoes and peas",
                Price = 4.99m,
                CategoryId = categoryAppetizers.Id,
                IsVegetarian = true,
                IsVegan = false,
                IsSpicy = true,
                SpiceLevel = 2,
                PreparationTime = 10,
                IsAvailable = true
            },
            new Product
            {
                Id = Guid.NewGuid(),
                Name = "Chicken Tikka",
                Description = "Tender chicken marinated in yogurt and spices, grilled to perfection",
                Price = 12.99m,
                CategoryId = categoryAppetizers.Id,
                IsVegetarian = false,
                IsVegan = false,
                IsSpicy = true,
                SpiceLevel = 3,
                PreparationTime = 15,
                IsAvailable = true
            },
            new Product
            {
                Id = Guid.NewGuid(),
                Name = "Paneer Tikka Masala",
                Description = "Cottage cheese cubes in rich tomato and cream sauce",
                Price = 13.99m,
                CategoryId = categoryMainCourse.Id,
                IsVegetarian = true,
                IsVegan = false,
                IsSpicy = true,
                SpiceLevel = 2,
                PreparationTime = 20,
                IsAvailable = true
            },
            new Product
            {
                Id = Guid.NewGuid(),
                Name = "Butter Chicken",
                Description = "Tender chicken in creamy tomato sauce with butter and spices",
                Price = 15.99m,
                CategoryId = categoryMainCourse.Id,
                IsVegetarian = false,
                IsVegan = false,
                IsSpicy = true,
                SpiceLevel = 2,
                PreparationTime = 25,
                IsAvailable = true
            },
            new Product
            {
                Id = Guid.NewGuid(),
                Name = "Chicken Biryani",
                Description = "Fragrant basmati rice cooked with chicken, herbs and spices",
                Price = 16.99m,
                CategoryId = categoryBiryani.Id,
                IsVegetarian = false,
                IsVegan = false,
                IsSpicy = true,
                SpiceLevel = 3,
                PreparationTime = 30,
                IsAvailable = true
            },
            new Product
            {
                Id = Guid.NewGuid(),
                Name = "Vegetable Biryani",
                Description = "Aromatic basmati rice with mixed vegetables and spices",
                Price = 13.99m,
                CategoryId = categoryBiryani.Id,
                IsVegetarian = true,
                IsVegan = true,
                IsSpicy = true,
                SpiceLevel = 2,
                PreparationTime = 25,
                IsAvailable = true
            },
            new Product
            {
                Id = Guid.NewGuid(),
                Name = "Gulab Jamun (3 pcs)",
                Description = "Soft milk dumplings soaked in rose-flavored sugar syrup",
                Price = 5.99m,
                CategoryId = categoryDesserts.Id,
                IsVegetarian = true,
                IsVegan = false,
                IsSpicy = false,
                SpiceLevel = 0,
                PreparationTime = 5,
                IsAvailable = true
            },
            new Product
            {
                Id = Guid.NewGuid(),
                Name = "Mango Lassi",
                Description = "Refreshing yogurt drink blended with sweet mangoes",
                Price = 4.99m,
                CategoryId = categoryBeverages.Id,
                IsVegetarian = true,
                IsVegan = false,
                IsSpicy = false,
                SpiceLevel = 0,
                PreparationTime = 5,
                IsAvailable = true
            },
            new Product
            {
                Id = Guid.NewGuid(),
                Name = "Masala Chai",
                Description = "Traditional Indian tea brewed with aromatic spices",
                Price = 2.99m,
                CategoryId = categoryBeverages.Id,
                IsVegetarian = true,
                IsVegan = false,
                IsSpicy = false,
                SpiceLevel = 0,
                PreparationTime = 5,
                IsAvailable = true
            }
        );
    }
}
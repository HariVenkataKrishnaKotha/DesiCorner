using DesiCorner.Services.CartAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace DesiCorner.Services.CartAPI.Data;

public class CartDbContext : DbContext
{
    public CartDbContext(DbContextOptions<CartDbContext> options) : base(options) { }

    public DbSet<Coupon> Coupons { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Coupon>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Code).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.DiscountAmount).HasColumnType("decimal(18,2)");
            entity.Property(e => e.MinAmount).HasColumnType("decimal(18,2)");
            entity.Property(e => e.MaxDiscount).HasColumnType("decimal(18,2)");
            entity.Property(e => e.DiscountType).HasMaxLength(20);

            // Unique constraint on code
            entity.HasIndex(e => e.Code).IsUnique();
        });

        // Seed default coupons
        SeedCoupons(modelBuilder);
    }

    private void SeedCoupons(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Coupon>().HasData(
            new Coupon
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Code = "WELCOME10",
                Description = "Welcome discount for new customers",
                DiscountAmount = 10,
                DiscountType = "Fixed",
                MinAmount = 0,
                IsActive = true,
                MaxUsageCount = 1000,
                UsedCount = 0,
                ExpiryDate = DateTime.UtcNow.AddMonths(12),
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "System"
            },
            new Coupon
            {
                Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                Code = "SAVE20",
                Description = "Save $20 on orders over $100",
                DiscountAmount = 20,
                DiscountType = "Fixed",
                MinAmount = 100,
                IsActive = true,
                MaxUsageCount = 500,
                UsedCount = 0,
                ExpiryDate = DateTime.UtcNow.AddMonths(6),
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "System"
            },
            new Coupon
            {
                Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                Code = "PERCENT15",
                Description = "15% off your order (max $50)",
                DiscountAmount = 15,
                DiscountType = "Percentage",
                MinAmount = 50,
                MaxDiscount = 50,
                IsActive = true,
                MaxUsageCount = 1000,
                UsedCount = 0,
                ExpiryDate = DateTime.UtcNow.AddMonths(3),
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "System"
            },
            new Coupon
            {
                Id = Guid.Parse("44444444-4444-4444-4444-444444444444"),
                Code = "FEAST50",
                Description = "Big feast discount - $50 off orders over $200",
                DiscountAmount = 50,
                DiscountType = "Fixed",
                MinAmount = 200,
                IsActive = true,
                MaxUsageCount = 100,
                UsedCount = 0,
                ExpiryDate = DateTime.UtcNow.AddDays(30),
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "System"
            }
        );
    }
}
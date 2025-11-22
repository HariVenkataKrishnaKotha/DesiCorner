using DesiCorner.Services.OrderAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace DesiCorner.Services.OrderAPI.Data;

public class OrderDbContext : DbContext
{
    public OrderDbContext(DbContextOptions<OrderDbContext> options) : base(options)
    {
    }

    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderItem> OrderItems { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Order configuration
        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.HasIndex(e => e.OrderNumber)
                  .IsUnique();

            entity.HasIndex(e => e.UserId);

            entity.HasIndex(e => e.Status);

            entity.HasIndex(e => e.OrderDate);

            // Configure one-to-many relationship
            entity.HasMany(e => e.Items)
                  .WithOne(e => e.Order)
                  .HasForeignKey(e => e.OrderId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // OrderItem configuration
        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.HasIndex(e => e.OrderId);

            entity.HasIndex(e => e.ProductId);
        });
    }
}
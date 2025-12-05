using DesiCorner.Services.PaymentAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace DesiCorner.Services.PaymentAPI.Data;

public class PaymentDbContext : DbContext
{
    public PaymentDbContext(DbContextOptions<PaymentDbContext> options) : base(options)
    {
    }

    public DbSet<Payment> Payments { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Payment>(entity =>
        {
            // Primary key
            entity.HasKey(p => p.Id);

            // Indexes for performance
            entity.HasIndex(p => p.PaymentIntentId)
                .IsUnique()
                .HasDatabaseName("IX_Payments_PaymentIntentId");

            entity.HasIndex(p => p.UserId)
                .HasDatabaseName("IX_Payments_UserId");

            entity.HasIndex(p => p.OrderId)
                .HasDatabaseName("IX_Payments_OrderId");

            entity.HasIndex(p => p.Status)
                .HasDatabaseName("IX_Payments_Status");

            // Decimal precision for monetary amounts
            entity.Property(p => p.Amount)
                .HasPrecision(18, 2);

            // Default values
            entity.Property(p => p.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            entity.Property(p => p.UpdatedAt)
                .HasDefaultValueSql("GETUTCDATE()");
        });
    }
}
using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace DesiCorner.Services.CartAPI.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Coupons",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DiscountAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DiscountType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    MinAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MaxDiscount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    ExpiryDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    MaxUsageCount = table.Column<int>(type: "int", nullable: false),
                    UsedCount = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Coupons", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "Coupons",
                columns: new[] { "Id", "Code", "CreatedAt", "CreatedBy", "Description", "DiscountAmount", "DiscountType", "ExpiryDate", "IsActive", "MaxDiscount", "MaxUsageCount", "MinAmount", "UpdatedAt", "UsedCount" },
                values: new object[,]
                {
                    { new Guid("11111111-1111-1111-1111-111111111111"), "WELCOME10", new DateTime(2025, 12, 18, 20, 43, 1, 657, DateTimeKind.Utc).AddTicks(8381), "System", "Welcome discount for new customers", 10m, "Fixed", new DateTime(2026, 12, 18, 20, 43, 1, 657, DateTimeKind.Utc).AddTicks(8369), true, null, 1000, 0m, null, 0 },
                    { new Guid("22222222-2222-2222-2222-222222222222"), "SAVE20", new DateTime(2025, 12, 18, 20, 43, 1, 657, DateTimeKind.Utc).AddTicks(8387), "System", "Save $20 on orders over $100", 20m, "Fixed", new DateTime(2026, 6, 18, 20, 43, 1, 657, DateTimeKind.Utc).AddTicks(8386), true, null, 500, 100m, null, 0 },
                    { new Guid("33333333-3333-3333-3333-333333333333"), "PERCENT15", new DateTime(2025, 12, 18, 20, 43, 1, 657, DateTimeKind.Utc).AddTicks(8396), "System", "15% off your order (max $50)", 15m, "Percentage", new DateTime(2026, 3, 18, 20, 43, 1, 657, DateTimeKind.Utc).AddTicks(8395), true, 50m, 1000, 50m, null, 0 },
                    { new Guid("44444444-4444-4444-4444-444444444444"), "FEAST50", new DateTime(2025, 12, 18, 20, 43, 1, 657, DateTimeKind.Utc).AddTicks(8411), "System", "Big feast discount - $50 off orders over $200", 50m, "Fixed", new DateTime(2026, 1, 17, 20, 43, 1, 657, DateTimeKind.Utc).AddTicks(8400), true, null, 100, 200m, null, 0 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Coupons_Code",
                table: "Coupons",
                column: "Code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Coupons");
        }
    }
}

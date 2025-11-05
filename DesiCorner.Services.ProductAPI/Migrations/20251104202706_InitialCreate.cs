using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace DesiCorner.Services.ProductAPI.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ImageUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Products",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ImageUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CategoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsAvailable = table.Column<bool>(type: "bit", nullable: false),
                    IsVegetarian = table.Column<bool>(type: "bit", nullable: false),
                    IsVegan = table.Column<bool>(type: "bit", nullable: false),
                    IsSpicy = table.Column<bool>(type: "bit", nullable: false),
                    SpiceLevel = table.Column<int>(type: "int", nullable: false),
                    Allergens = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    PreparationTime = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Products", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Products_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "Categories",
                columns: new[] { "Id", "CreatedAt", "Description", "DisplayOrder", "ImageUrl", "Name" },
                values: new object[,]
                {
                    { new Guid("11111111-1111-1111-1111-111111111111"), new DateTime(2025, 11, 4, 20, 27, 4, 364, DateTimeKind.Utc).AddTicks(8712), "Start your meal with these delicious appetizers", 1, null, "Appetizers" },
                    { new Guid("22222222-2222-2222-2222-222222222222"), new DateTime(2025, 11, 4, 20, 27, 4, 364, DateTimeKind.Utc).AddTicks(8714), "Hearty main dishes to satisfy your hunger", 2, null, "Main Course" },
                    { new Guid("33333333-3333-3333-3333-333333333333"), new DateTime(2025, 11, 4, 20, 27, 4, 364, DateTimeKind.Utc).AddTicks(8715), "Aromatic rice dishes with your choice of protein", 3, null, "Biryani" },
                    { new Guid("44444444-4444-4444-4444-444444444444"), new DateTime(2025, 11, 4, 20, 27, 4, 364, DateTimeKind.Utc).AddTicks(8718), "Sweet endings to your perfect meal", 4, null, "Desserts" },
                    { new Guid("55555555-5555-5555-5555-555555555555"), new DateTime(2025, 11, 4, 20, 27, 4, 364, DateTimeKind.Utc).AddTicks(8721), "Refreshing drinks to complement your meal", 5, null, "Beverages" }
                });

            migrationBuilder.InsertData(
                table: "Products",
                columns: new[] { "Id", "Allergens", "CategoryId", "CreatedAt", "Description", "ImageUrl", "IsAvailable", "IsSpicy", "IsVegan", "IsVegetarian", "Name", "PreparationTime", "Price", "SpiceLevel", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("2115f9b3-ff26-4a30-af88-052e7d3f5f07"), null, new Guid("33333333-3333-3333-3333-333333333333"), new DateTime(2025, 11, 4, 20, 27, 4, 364, DateTimeKind.Utc).AddTicks(8872), "Fragrant basmati rice cooked with chicken, herbs and spices", null, true, true, false, false, "Chicken Biryani", 30, 16.99m, 3, null },
                    { new Guid("2dcd0793-899e-40c1-aa73-67e2be8ed904"), null, new Guid("11111111-1111-1111-1111-111111111111"), new DateTime(2025, 11, 4, 20, 27, 4, 364, DateTimeKind.Utc).AddTicks(8827), "Crispy pastry filled with spiced potatoes and peas", null, true, true, false, true, "Samosa (2 pcs)", 10, 4.99m, 2, null },
                    { new Guid("48841368-3764-427e-966f-bbc54c091b70"), null, new Guid("22222222-2222-2222-2222-222222222222"), new DateTime(2025, 11, 4, 20, 27, 4, 364, DateTimeKind.Utc).AddTicks(8870), "Tender chicken in creamy tomato sauce with butter and spices", null, true, true, false, false, "Butter Chicken", 25, 15.99m, 2, null },
                    { new Guid("6569f699-da0e-4008-8436-150dbcc70641"), null, new Guid("44444444-4444-4444-4444-444444444444"), new DateTime(2025, 11, 4, 20, 27, 4, 364, DateTimeKind.Utc).AddTicks(8884), "Soft milk dumplings soaked in rose-flavored sugar syrup", null, true, false, false, true, "Gulab Jamun (3 pcs)", 5, 5.99m, 0, null },
                    { new Guid("78f46fc3-c2b2-44a3-810c-20bc2da9eb84"), null, new Guid("33333333-3333-3333-3333-333333333333"), new DateTime(2025, 11, 4, 20, 27, 4, 364, DateTimeKind.Utc).AddTicks(8874), "Aromatic basmati rice with mixed vegetables and spices", null, true, true, true, true, "Vegetable Biryani", 25, 13.99m, 2, null },
                    { new Guid("b97ba28c-15fa-4e89-a93b-8289254af2a6"), null, new Guid("55555555-5555-5555-5555-555555555555"), new DateTime(2025, 11, 4, 20, 27, 4, 364, DateTimeKind.Utc).AddTicks(8886), "Refreshing yogurt drink blended with sweet mangoes", null, true, false, false, true, "Mango Lassi", 5, 4.99m, 0, null },
                    { new Guid("d3f7c900-ab73-438e-9e2c-7020dd0e0d8c"), null, new Guid("55555555-5555-5555-5555-555555555555"), new DateTime(2025, 11, 4, 20, 27, 4, 364, DateTimeKind.Utc).AddTicks(8890), "Traditional Indian tea brewed with aromatic spices", null, true, false, false, true, "Masala Chai", 5, 2.99m, 0, null },
                    { new Guid("f85c305d-4780-443d-9ab6-339d72227470"), null, new Guid("22222222-2222-2222-2222-222222222222"), new DateTime(2025, 11, 4, 20, 27, 4, 364, DateTimeKind.Utc).AddTicks(8868), "Cottage cheese cubes in rich tomato and cream sauce", null, true, true, false, true, "Paneer Tikka Masala", 20, 13.99m, 2, null },
                    { new Guid("fa0bfe8a-423d-4ef9-9f9f-7f8e9ada2747"), null, new Guid("11111111-1111-1111-1111-111111111111"), new DateTime(2025, 11, 4, 20, 27, 4, 364, DateTimeKind.Utc).AddTicks(8865), "Tender chicken marinated in yogurt and spices, grilled to perfection", null, true, true, false, false, "Chicken Tikka", 15, 12.99m, 3, null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Categories_Name",
                table: "Categories",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Products_CategoryId",
                table: "Products",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Products_Name",
                table: "Products",
                column: "Name");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Products");

            migrationBuilder.DropTable(
                name: "Categories");
        }
    }
}

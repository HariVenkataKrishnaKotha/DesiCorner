using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace DesiCorner.Services.ProductAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddReviewsAndRatings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("2115f9b3-ff26-4a30-af88-052e7d3f5f07"));

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("2dcd0793-899e-40c1-aa73-67e2be8ed904"));

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("48841368-3764-427e-966f-bbc54c091b70"));

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("6569f699-da0e-4008-8436-150dbcc70641"));

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("78f46fc3-c2b2-44a3-810c-20bc2da9eb84"));

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("b97ba28c-15fa-4e89-a93b-8289254af2a6"));

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("d3f7c900-ab73-438e-9e2c-7020dd0e0d8c"));

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("f85c305d-4780-443d-9ab6-339d72227470"));

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("fa0bfe8a-423d-4ef9-9f9f-7f8e9ada2747"));

            migrationBuilder.AddColumn<double>(
                name: "AverageRating",
                table: "Products",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<int>(
                name: "ReviewCount",
                table: "Products",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Reviews",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    UserEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Rating = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Comment = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    IsVerifiedPurchase = table.Column<bool>(type: "bit", nullable: false),
                    IsApproved = table.Column<bool>(type: "bit", nullable: false),
                    HelpfulCount = table.Column<int>(type: "int", nullable: false),
                    NotHelpfulCount = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reviews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Reviews_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                column: "CreatedAt",
                value: new DateTime(2025, 12, 16, 21, 16, 44, 733, DateTimeKind.Utc).AddTicks(2523));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                column: "CreatedAt",
                value: new DateTime(2025, 12, 16, 21, 16, 44, 733, DateTimeKind.Utc).AddTicks(2526));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                column: "CreatedAt",
                value: new DateTime(2025, 12, 16, 21, 16, 44, 733, DateTimeKind.Utc).AddTicks(2528));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444444"),
                column: "CreatedAt",
                value: new DateTime(2025, 12, 16, 21, 16, 44, 733, DateTimeKind.Utc).AddTicks(2531));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("55555555-5555-5555-5555-555555555555"),
                column: "CreatedAt",
                value: new DateTime(2025, 12, 16, 21, 16, 44, 733, DateTimeKind.Utc).AddTicks(2532));

            migrationBuilder.InsertData(
                table: "Products",
                columns: new[] { "Id", "Allergens", "AverageRating", "CategoryId", "CreatedAt", "Description", "ImageUrl", "IsAvailable", "IsSpicy", "IsVegan", "IsVegetarian", "Name", "PreparationTime", "Price", "ReviewCount", "SpiceLevel", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("43fada9a-9583-457f-93df-5d7fcb175bf0"), null, 0.0, new Guid("11111111-1111-1111-1111-111111111111"), new DateTime(2025, 12, 16, 21, 16, 44, 733, DateTimeKind.Utc).AddTicks(2731), "Tender chicken marinated in yogurt and spices, grilled to perfection", null, true, true, false, false, "Chicken Tikka", 15, 12.99m, 0, 3, null },
                    { new Guid("5288829d-e04c-454f-a6aa-cc9ed067183c"), null, 0.0, new Guid("55555555-5555-5555-5555-555555555555"), new DateTime(2025, 12, 16, 21, 16, 44, 733, DateTimeKind.Utc).AddTicks(2757), "Refreshing yogurt drink blended with sweet mangoes", null, true, false, false, true, "Mango Lassi", 5, 4.99m, 0, 0, null },
                    { new Guid("5e00f505-c687-4470-a6d0-a8856d01d3d1"), null, 0.0, new Guid("11111111-1111-1111-1111-111111111111"), new DateTime(2025, 12, 16, 21, 16, 44, 733, DateTimeKind.Utc).AddTicks(2647), "Crispy pastry filled with spiced potatoes and peas", null, true, true, false, true, "Samosa (2 pcs)", 10, 4.99m, 0, 2, null },
                    { new Guid("65e5a6e7-a5f5-4521-9a70-ea786e874dfc"), null, 0.0, new Guid("44444444-4444-4444-4444-444444444444"), new DateTime(2025, 12, 16, 21, 16, 44, 733, DateTimeKind.Utc).AddTicks(2751), "Soft milk dumplings soaked in rose-flavored sugar syrup", null, true, false, false, true, "Gulab Jamun (3 pcs)", 5, 5.99m, 0, 0, null },
                    { new Guid("9318d852-74ca-4048-b0b0-c979d43a6c19"), null, 0.0, new Guid("33333333-3333-3333-3333-333333333333"), new DateTime(2025, 12, 16, 21, 16, 44, 733, DateTimeKind.Utc).AddTicks(2748), "Aromatic basmati rice with mixed vegetables and spices", null, true, true, true, true, "Vegetable Biryani", 25, 13.99m, 0, 2, null },
                    { new Guid("94cd611f-1b0f-4cf6-b50d-a16fc9d8306e"), null, 0.0, new Guid("22222222-2222-2222-2222-222222222222"), new DateTime(2025, 12, 16, 21, 16, 44, 733, DateTimeKind.Utc).AddTicks(2743), "Tender chicken in creamy tomato sauce with butter and spices", null, true, true, false, false, "Butter Chicken", 25, 15.99m, 0, 2, null },
                    { new Guid("9db5a77c-f4f1-4cbf-828f-025a68ca41e5"), null, 0.0, new Guid("55555555-5555-5555-5555-555555555555"), new DateTime(2025, 12, 16, 21, 16, 44, 733, DateTimeKind.Utc).AddTicks(2762), "Traditional Indian tea brewed with aromatic spices", null, true, false, false, true, "Masala Chai", 5, 2.99m, 0, 0, null },
                    { new Guid("bb7e26fd-a14c-4153-9299-fa6d77236c1d"), null, 0.0, new Guid("22222222-2222-2222-2222-222222222222"), new DateTime(2025, 12, 16, 21, 16, 44, 733, DateTimeKind.Utc).AddTicks(2734), "Cottage cheese cubes in rich tomato and cream sauce", null, true, true, false, true, "Paneer Tikka Masala", 20, 13.99m, 0, 2, null },
                    { new Guid("cdabf734-23b1-408e-9d2e-4ef875413e82"), null, 0.0, new Guid("33333333-3333-3333-3333-333333333333"), new DateTime(2025, 12, 16, 21, 16, 44, 733, DateTimeKind.Utc).AddTicks(2746), "Fragrant basmati rice cooked with chicken, herbs and spices", null, true, true, false, false, "Chicken Biryani", 30, 16.99m, 0, 3, null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_CreatedAt",
                table: "Reviews",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_ProductId",
                table: "Reviews",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_ProductId_UserId",
                table: "Reviews",
                columns: new[] { "ProductId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_UserId",
                table: "Reviews",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Reviews");

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("43fada9a-9583-457f-93df-5d7fcb175bf0"));

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("5288829d-e04c-454f-a6aa-cc9ed067183c"));

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("5e00f505-c687-4470-a6d0-a8856d01d3d1"));

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("65e5a6e7-a5f5-4521-9a70-ea786e874dfc"));

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("9318d852-74ca-4048-b0b0-c979d43a6c19"));

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("94cd611f-1b0f-4cf6-b50d-a16fc9d8306e"));

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("9db5a77c-f4f1-4cbf-828f-025a68ca41e5"));

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("bb7e26fd-a14c-4153-9299-fa6d77236c1d"));

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("cdabf734-23b1-408e-9d2e-4ef875413e82"));

            migrationBuilder.DropColumn(
                name: "AverageRating",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "ReviewCount",
                table: "Products");

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                column: "CreatedAt",
                value: new DateTime(2025, 11, 4, 20, 27, 4, 364, DateTimeKind.Utc).AddTicks(8712));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                column: "CreatedAt",
                value: new DateTime(2025, 11, 4, 20, 27, 4, 364, DateTimeKind.Utc).AddTicks(8714));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                column: "CreatedAt",
                value: new DateTime(2025, 11, 4, 20, 27, 4, 364, DateTimeKind.Utc).AddTicks(8715));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444444"),
                column: "CreatedAt",
                value: new DateTime(2025, 11, 4, 20, 27, 4, 364, DateTimeKind.Utc).AddTicks(8718));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("55555555-5555-5555-5555-555555555555"),
                column: "CreatedAt",
                value: new DateTime(2025, 11, 4, 20, 27, 4, 364, DateTimeKind.Utc).AddTicks(8721));

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
        }
    }
}

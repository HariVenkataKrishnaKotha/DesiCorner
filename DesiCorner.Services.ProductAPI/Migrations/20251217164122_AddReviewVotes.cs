using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace DesiCorner.Services.ProductAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddReviewVotes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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

            migrationBuilder.CreateTable(
                name: "ReviewVotes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReviewId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsHelpful = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReviewVotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReviewVotes_Reviews_ReviewId",
                        column: x => x.ReviewId,
                        principalTable: "Reviews",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                column: "CreatedAt",
                value: new DateTime(2025, 12, 17, 16, 41, 20, 586, DateTimeKind.Utc).AddTicks(2485));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                column: "CreatedAt",
                value: new DateTime(2025, 12, 17, 16, 41, 20, 586, DateTimeKind.Utc).AddTicks(2487));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                column: "CreatedAt",
                value: new DateTime(2025, 12, 17, 16, 41, 20, 586, DateTimeKind.Utc).AddTicks(2489));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444444"),
                column: "CreatedAt",
                value: new DateTime(2025, 12, 17, 16, 41, 20, 586, DateTimeKind.Utc).AddTicks(2491));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("55555555-5555-5555-5555-555555555555"),
                column: "CreatedAt",
                value: new DateTime(2025, 12, 17, 16, 41, 20, 586, DateTimeKind.Utc).AddTicks(2492));

            migrationBuilder.InsertData(
                table: "Products",
                columns: new[] { "Id", "Allergens", "AverageRating", "CategoryId", "CreatedAt", "Description", "ImageUrl", "IsAvailable", "IsSpicy", "IsVegan", "IsVegetarian", "Name", "PreparationTime", "Price", "ReviewCount", "SpiceLevel", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("110b0658-26f9-4d30-9e07-c7ec8b5feec1"), null, 0.0, new Guid("33333333-3333-3333-3333-333333333333"), new DateTime(2025, 12, 17, 16, 41, 20, 586, DateTimeKind.Utc).AddTicks(2670), "Aromatic basmati rice with mixed vegetables and spices", null, true, true, true, true, "Vegetable Biryani", 25, 13.99m, 0, 2, null },
                    { new Guid("236f8a89-e475-43d7-a875-5c1b2dc82ca7"), null, 0.0, new Guid("44444444-4444-4444-4444-444444444444"), new DateTime(2025, 12, 17, 16, 41, 20, 586, DateTimeKind.Utc).AddTicks(2672), "Soft milk dumplings soaked in rose-flavored sugar syrup", null, true, false, false, true, "Gulab Jamun (3 pcs)", 5, 5.99m, 0, 0, null },
                    { new Guid("4d30f61b-0ebe-45a5-ac5d-7c269278f9a3"), null, 0.0, new Guid("55555555-5555-5555-5555-555555555555"), new DateTime(2025, 12, 17, 16, 41, 20, 586, DateTimeKind.Utc).AddTicks(2677), "Traditional Indian tea brewed with aromatic spices", null, true, false, false, true, "Masala Chai", 5, 2.99m, 0, 0, null },
                    { new Guid("658e8094-8080-45ad-b4b2-e5e8652b0cb5"), null, 0.0, new Guid("22222222-2222-2222-2222-222222222222"), new DateTime(2025, 12, 17, 16, 41, 20, 586, DateTimeKind.Utc).AddTicks(2657), "Cottage cheese cubes in rich tomato and cream sauce", null, true, true, false, true, "Paneer Tikka Masala", 20, 13.99m, 0, 2, null },
                    { new Guid("a3b32634-6fcd-464c-8cf5-ffbcf1a1fe99"), null, 0.0, new Guid("11111111-1111-1111-1111-111111111111"), new DateTime(2025, 12, 17, 16, 41, 20, 586, DateTimeKind.Utc).AddTicks(2617), "Crispy pastry filled with spiced potatoes and peas", null, true, true, false, true, "Samosa (2 pcs)", 10, 4.99m, 0, 2, null },
                    { new Guid("af1e79ed-a5b9-4426-87e0-5359344abdba"), null, 0.0, new Guid("55555555-5555-5555-5555-555555555555"), new DateTime(2025, 12, 17, 16, 41, 20, 586, DateTimeKind.Utc).AddTicks(2674), "Refreshing yogurt drink blended with sweet mangoes", null, true, false, false, true, "Mango Lassi", 5, 4.99m, 0, 0, null },
                    { new Guid("d28b2fa2-1c39-4281-9887-e42836c3c9f1"), null, 0.0, new Guid("22222222-2222-2222-2222-222222222222"), new DateTime(2025, 12, 17, 16, 41, 20, 586, DateTimeKind.Utc).AddTicks(2660), "Tender chicken in creamy tomato sauce with butter and spices", null, true, true, false, false, "Butter Chicken", 25, 15.99m, 0, 2, null },
                    { new Guid("e9c98de7-20c9-4694-9e62-42b7eba0879a"), null, 0.0, new Guid("11111111-1111-1111-1111-111111111111"), new DateTime(2025, 12, 17, 16, 41, 20, 586, DateTimeKind.Utc).AddTicks(2654), "Tender chicken marinated in yogurt and spices, grilled to perfection", null, true, true, false, false, "Chicken Tikka", 15, 12.99m, 0, 3, null },
                    { new Guid("f99c4d95-32e9-4827-a8e5-dcfbec0fce76"), null, 0.0, new Guid("33333333-3333-3333-3333-333333333333"), new DateTime(2025, 12, 17, 16, 41, 20, 586, DateTimeKind.Utc).AddTicks(2667), "Fragrant basmati rice cooked with chicken, herbs and spices", null, true, true, false, false, "Chicken Biryani", 30, 16.99m, 0, 3, null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_ReviewVotes_ReviewId_UserId",
                table: "ReviewVotes",
                columns: new[] { "ReviewId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReviewVotes_UserId",
                table: "ReviewVotes",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReviewVotes");

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("110b0658-26f9-4d30-9e07-c7ec8b5feec1"));

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("236f8a89-e475-43d7-a875-5c1b2dc82ca7"));

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("4d30f61b-0ebe-45a5-ac5d-7c269278f9a3"));

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("658e8094-8080-45ad-b4b2-e5e8652b0cb5"));

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("a3b32634-6fcd-464c-8cf5-ffbcf1a1fe99"));

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("af1e79ed-a5b9-4426-87e0-5359344abdba"));

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("d28b2fa2-1c39-4281-9887-e42836c3c9f1"));

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("e9c98de7-20c9-4694-9e62-42b7eba0879a"));

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("f99c4d95-32e9-4827-a8e5-dcfbec0fce76"));

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
        }
    }
}

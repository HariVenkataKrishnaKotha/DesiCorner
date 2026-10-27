using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DesiCorner.Services.OrderAPI.Migrations
{
    /// <inheritdoc />
    public partial class UniquePaymentIntentPerOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Orders_PaymentIntentId",
                table: "Orders",
                column: "PaymentIntentId",
                unique: true,
                filter: "[PaymentIntentId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Orders_PaymentIntentId",
                table: "Orders");
        }
    }
}

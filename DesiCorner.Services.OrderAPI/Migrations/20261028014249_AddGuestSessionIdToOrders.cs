using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DesiCorner.Services.OrderAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddGuestSessionIdToOrders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "GuestSessionId",
                table: "Orders",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GuestSessionId",
                table: "Orders");
        }
    }
}

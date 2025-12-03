using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DesiCorner.Services.OrderAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddGuestOrderSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsGuestOrder",
                table: "Orders",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsGuestOrder",
                table: "Orders");
        }
    }
}

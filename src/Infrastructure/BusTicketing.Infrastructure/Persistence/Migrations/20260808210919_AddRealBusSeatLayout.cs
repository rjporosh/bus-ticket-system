using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BusTicketing.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRealBusSeatLayout : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsDriver",
                table: "Seats",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "LayoutConfigJson",
                table: "SeatLayouts",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LayoutType",
                table: "SeatLayouts",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsDriver",
                table: "Seats");

            migrationBuilder.DropColumn(
                name: "LayoutConfigJson",
                table: "SeatLayouts");

            migrationBuilder.DropColumn(
                name: "LayoutType",
                table: "SeatLayouts");
        }
    }
}

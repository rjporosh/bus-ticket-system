using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BusTicketing.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAgeToTickets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Age",
                table: "Tickets",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Age",
                table: "Tickets");
        }
    }
}

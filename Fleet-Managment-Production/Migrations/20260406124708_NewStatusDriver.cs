using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fleet_Managment_Production.Migrations
{
    /// <inheritdoc />
    public partial class NewStatusDriver : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Drivers",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Status",
                table: "Drivers");
        }
    }
}

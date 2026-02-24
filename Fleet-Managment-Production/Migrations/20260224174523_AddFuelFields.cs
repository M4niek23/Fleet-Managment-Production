using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fleet_Managment_Production.Migrations
{
    /// <inheritdoc />
    public partial class AddFuelFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Opis",
                table: "Costs",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(250)",
                oldMaxLength: 250);

            migrationBuilder.AddColumn<int>(
                name: "CurrentOdometer",
                table: "Costs",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsFullTank",
                table: "Costs",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<double>(
                name: "Liters",
                table: "Costs",
                type: "float",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CurrentOdometer",
                table: "Costs");

            migrationBuilder.DropColumn(
                name: "IsFullTank",
                table: "Costs");

            migrationBuilder.DropColumn(
                name: "Liters",
                table: "Costs");

            migrationBuilder.AlterColumn<string>(
                name: "Opis",
                table: "Costs",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);
        }
    }
}

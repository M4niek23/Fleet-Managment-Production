using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fleet_Managment_Production.Migrations
{
    /// <inheritdoc />
    public partial class UpdateLicenseCategoryToMultiSelect : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LicenseCategory",
                table: "Drivers");

            migrationBuilder.AddColumn<string>(
                name: "LicenseCategories",
                table: "Drivers",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LicenseCategories",
                table: "Drivers");

            migrationBuilder.AddColumn<int>(
                name: "LicenseCategory",
                table: "Drivers",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}

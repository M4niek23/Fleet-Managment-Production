using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fleet_Managment_Production.Migrations
{
    /// <inheritdoc />
    public partial class PoprawkaWBazieWmodeluCost : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Opis",
                table: "Costs",
                newName: "Description");

            migrationBuilder.RenameColumn(
                name: "Kwota",
                table: "Costs",
                newName: "Amount");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Description",
                table: "Costs",
                newName: "Opis");

            migrationBuilder.RenameColumn(
                name: "Amount",
                table: "Costs",
                newName: "Kwota");
        }
    }
}

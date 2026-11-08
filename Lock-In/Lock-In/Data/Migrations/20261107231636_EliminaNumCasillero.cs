using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lock_In.Migrations
{
    /// <inheritdoc />
    public partial class EliminaNumCasillero : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Numero",
                table: "Casilleros");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Numero",
                table: "Casilleros",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}

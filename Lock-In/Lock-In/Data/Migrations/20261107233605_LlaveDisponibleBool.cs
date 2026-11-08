using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lock_In.Migrations
{
    /// <inheritdoc />
    public partial class LlaveDisponibleBool : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "LlaveDisponible",
                table: "Casilleros",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LlaveDisponible",
                table: "Casilleros");
        }
    }
}

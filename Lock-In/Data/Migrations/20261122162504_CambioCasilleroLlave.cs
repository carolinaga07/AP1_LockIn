using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lock_In.Infrastructure.Migrations;

/// <inheritdoc />
public partial class _20261122162504_CambioCasilleroLlave : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_Llaves_CasilleroId",
            table: "Llaves");

        migrationBuilder.AlterColumn<int>(
            name: "Numero",
            table: "Llaves",
            type: "int",
            nullable: false,
            oldClrType: typeof(string),
            oldType: "nvarchar(max)");

        migrationBuilder.CreateIndex(
            name: "IX_Llaves_CasilleroId",
            table: "Llaves",
            column: "CasilleroId",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_Llaves_CasilleroId",
            table: "Llaves");

        migrationBuilder.AlterColumn<string>(
            name: "Numero",
            table: "Llaves",
            type: "nvarchar(max)",
            nullable: false,
            oldClrType: typeof(int),
            oldType: "int");

        migrationBuilder.CreateIndex(
            name: "IX_Llaves_CasilleroId",
            table: "Llaves",
            column: "CasilleroId");
    }
}

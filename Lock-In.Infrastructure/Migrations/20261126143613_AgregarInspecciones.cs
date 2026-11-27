using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lock_In.Infrastructure.Migrations;

/// <inheritdoc />
public partial class _20261126143613_AgregarInspecciones : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Inspecciones",
            columns: table => new
            {
                InspeccionId = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                Fecha = table.Column<DateTime>(type: "datetime2", nullable: false),
                ResponsableId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                Observaciones = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                Estado = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Inspecciones", x => x.InspeccionId);
                table.ForeignKey(
                    name: "FK_Inspecciones_AspNetUsers_ResponsableId",
                    column: x => x.ResponsableId,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "DetalleInspecciones",
            columns: table => new
            {
                DetalleId = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                InspeccionId = table.Column<int>(type: "int", nullable: false),
                CasilleroId = table.Column<int>(type: "int", nullable: false),
                Condicion = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                Observaciones = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                RequiereMantenimiento = table.Column<bool>(type: "bit", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_DetalleInspecciones", x => x.DetalleId);
                table.ForeignKey(
                    name: "FK_DetalleInspecciones_Casilleros_CasilleroId",
                    column: x => x.CasilleroId,
                    principalTable: "Casilleros",
                    principalColumn: "CasilleroId",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_DetalleInspecciones_Inspecciones_InspeccionId",
                    column: x => x.InspeccionId,
                    principalTable: "Inspecciones",
                    principalColumn: "InspeccionId",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_DetalleInspecciones_CasilleroId",
            table: "DetalleInspecciones",
            column: "CasilleroId");

        migrationBuilder.CreateIndex(
            name: "IX_DetalleInspecciones_InspeccionId",
            table: "DetalleInspecciones",
            column: "InspeccionId");

        migrationBuilder.CreateIndex(
            name: "IX_Inspecciones_ResponsableId",
            table: "Inspecciones",
            column: "ResponsableId");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "DetalleInspecciones");

        migrationBuilder.DropTable(
            name: "Inspecciones");
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lock_In.Infrastructure.Migrations;

/// <inheritdoc />
public partial class _20261123132759_AgregarUrlNotificaciones : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "Url",
            table: "Notificaciones",
            type: "nvarchar(max)",
            nullable: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "Url",
            table: "Notificaciones");
    }
}

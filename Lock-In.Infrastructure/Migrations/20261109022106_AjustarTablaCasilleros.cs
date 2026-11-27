using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lock_In.Migrations
{
    /// <inheritdoc />
    public partial class AjustarTablaCasilleros : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF COL_LENGTH(N'dbo.Casilleros', N'Numero') IS NULL
                BEGIN
                    ALTER TABLE [dbo].[Casilleros]
                    ADD [Numero] int NOT NULL DEFAULT (0);
                END
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}

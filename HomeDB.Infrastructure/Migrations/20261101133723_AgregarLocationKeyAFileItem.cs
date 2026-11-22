using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HomeDB.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AgregarLocationKeyAFileItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            //Los archivos ya existentes pasan a la ubicación "main" sin moverse, tal como define el módulo de
            //almacenamiento multi-ubicación (nunca se renombran claves de ubicación).
            migrationBuilder.AddColumn<string>(
                name: "location_key",
                table: "file_items",
                type: "text",
                nullable: false,
                defaultValue: "main");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "location_key",
                table: "file_items");
        }
    }
}

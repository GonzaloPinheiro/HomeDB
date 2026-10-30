using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HomeDB.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AgregarModuloTorrents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "torrents_enabled",
                table: "user_module_permissions",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "torrents_enabled",
                table: "user_module_permissions");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HomeDB.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AgregarProcessingYResultadoAUploadSession : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "error_code",
                schema: "uploads",
                table: "upload_sessions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "result_file_item_id",
                schema: "uploads",
                table: "upload_sessions",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "error_code",
                schema: "uploads",
                table: "upload_sessions");

            migrationBuilder.DropColumn(
                name: "result_file_item_id",
                schema: "uploads",
                table: "upload_sessions");
        }
    }
}

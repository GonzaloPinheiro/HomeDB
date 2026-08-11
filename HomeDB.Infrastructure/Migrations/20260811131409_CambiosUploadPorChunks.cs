using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HomeDB.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CambiosUploadPorChunks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "last_activity_at",
                schema: "uploads",
                table: "upload_sessions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "max_file_size_bytes",
                schema: "uploads",
                table: "upload_sessions",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "received_size_bytes",
                schema: "uploads",
                table: "upload_sessions",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "size_bytes",
                schema: "uploads",
                table: "upload_chunks",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "last_activity_at",
                schema: "uploads",
                table: "upload_sessions");

            migrationBuilder.DropColumn(
                name: "max_file_size_bytes",
                schema: "uploads",
                table: "upload_sessions");

            migrationBuilder.DropColumn(
                name: "received_size_bytes",
                schema: "uploads",
                table: "upload_sessions");

            migrationBuilder.DropColumn(
                name: "size_bytes",
                schema: "uploads",
                table: "upload_chunks");
        }
    }
}

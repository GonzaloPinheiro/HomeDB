using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace HomeDB.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AgregarTablasParaUploadConChunks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "uploads");

            migrationBuilder.CreateTable(
                name: "upload_sessions",
                schema: "uploads",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    file_name = table.Column<string>(type: "text", nullable: false),
                    total_size_bytes = table.Column<long>(type: "bigint", nullable: false),
                    total_chunks = table.Column<int>(type: "integer", nullable: false),
                    owner_id = table.Column<int>(type: "integer", nullable: false),
                    folder_id = table.Column<int>(type: "integer", nullable: true),
                    status = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_upload_sessions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "upload_chunks",
                schema: "uploads",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    upload_session_id = table.Column<int>(type: "integer", nullable: false),
                    chunk_number = table.Column<int>(type: "integer", nullable: false),
                    received_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_upload_chunks", x => x.id);
                    table.ForeignKey(
                        name: "fk_upload_chunks_upload_sessions_upload_session_id",
                        column: x => x.upload_session_id,
                        principalSchema: "uploads",
                        principalTable: "upload_sessions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_upload_chunks_upload_session_id_chunk_number",
                schema: "uploads",
                table: "upload_chunks",
                columns: new[] { "upload_session_id", "chunk_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_upload_sessions_session_id",
                schema: "uploads",
                table: "upload_sessions",
                column: "session_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "upload_chunks",
                schema: "uploads");

            migrationBuilder.DropTable(
                name: "upload_sessions",
                schema: "uploads");
        }
    }
}

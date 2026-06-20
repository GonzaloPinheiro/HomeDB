using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace HomeDB.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AgregarSystemMetrics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "system_metrics_entries",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    cpu_usage_percent = table.Column<double>(type: "double precision", nullable: true),
                    memory_total_bytes = table.Column<long>(type: "bigint", nullable: true),
                    memory_used_bytes = table.Column<long>(type: "bigint", nullable: true),
                    memory_usage_percent = table.Column<double>(type: "double precision", nullable: true),
                    disk_total_bytes = table.Column<long>(type: "bigint", nullable: true),
                    disk_used_bytes = table.Column<long>(type: "bigint", nullable: true),
                    disk_usage_percent = table.Column<double>(type: "double precision", nullable: true),
                    temperature_celsius = table.Column<double>(type: "double precision", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_system_metrics_entries", x => x.id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "system_metrics_entries");
        }
    }
}

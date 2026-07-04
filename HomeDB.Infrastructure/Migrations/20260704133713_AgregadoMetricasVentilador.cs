using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HomeDB.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AgregadoMetricasVentilador : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "fan_control_mode",
                table: "system_metrics_entries",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "fan_is_running",
                table: "system_metrics_entries",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "fan_pwm_duty_cycle",
                table: "system_metrics_entries",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "fan_rpm_speed",
                table: "system_metrics_entries",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "fan_control_mode",
                table: "system_metrics_entries");

            migrationBuilder.DropColumn(
                name: "fan_is_running",
                table: "system_metrics_entries");

            migrationBuilder.DropColumn(
                name: "fan_pwm_duty_cycle",
                table: "system_metrics_entries");

            migrationBuilder.DropColumn(
                name: "fan_rpm_speed",
                table: "system_metrics_entries");
        }
    }
}

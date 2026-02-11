using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FocusTrack.Session.Infrastructure.Migrations
{
    public partial class AddIsDailyGoalAchievedAndDurationMin : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "DurationMin",
                table: "Sessions",
                type: "numeric(5,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "IsDailyGoalAchieved",
                table: "Sessions",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DurationMin",
                table: "Sessions");

            migrationBuilder.DropColumn(
                name: "IsDailyGoalAchieved",
                table: "Sessions");
        }
    }
}

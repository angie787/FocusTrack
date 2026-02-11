using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace FocusTrack.RewardWorker.Migrations
{
    public partial class InitialRewards : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DailyFocusContributions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    CalendarDate = table.Column<DateOnly>(type: "date", nullable: false),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    DurationMin = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DailyFocusContributions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DailyGoalAchievements",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    CalendarDate = table.Column<DateOnly>(type: "date", nullable: false),
                    TriggeringSessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    AchievedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DailyGoalAchievements", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DailyFocusContributions_UserId_CalendarDate_SessionId",
                table: "DailyFocusContributions",
                columns: new[] { "UserId", "CalendarDate", "SessionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DailyGoalAchievements_UserId_CalendarDate",
                table: "DailyGoalAchievements",
                columns: new[] { "UserId", "CalendarDate" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "DailyFocusContributions");
            migrationBuilder.DropTable(name: "DailyGoalAchievements");
        }
    }
}

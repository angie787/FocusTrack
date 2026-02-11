using Microsoft.EntityFrameworkCore;

namespace FocusTrack.RewardWorker.Persistence;

public class RewardsDbContext : DbContext
{
    public RewardsDbContext(DbContextOptions<RewardsDbContext> options) : base(options) { }

    public DbSet<DailyFocusContribution> DailyFocusContributions => Set<DailyFocusContribution>();
    public DbSet<DailyGoalAchievement> DailyGoalAchievements => Set<DailyGoalAchievement>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DailyFocusContribution>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedOnAdd();
            e.Property(x => x.UserId).HasMaxLength(256);
            e.Property(x => x.DurationMin).HasPrecision(5, 2);
            e.HasIndex(x => new { x.UserId, x.CalendarDate, x.SessionId }).IsUnique();
        });

        modelBuilder.Entity<DailyGoalAchievement>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedOnAdd();
            e.Property(x => x.UserId).HasMaxLength(256);
            e.HasIndex(x => new { x.UserId, x.CalendarDate }).IsUnique();
        });
    }
}

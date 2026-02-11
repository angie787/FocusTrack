using Microsoft.EntityFrameworkCore;
using FocusTrack.Session.Domain.Models;

namespace FocusTrack.Session.Infrastructure.Persistence;

public class SessionDbContext : DbContext
{
    public SessionDbContext(DbContextOptions<SessionDbContext> options) : base(options) { }
    public DbSet<Domain.Models.Session> Sessions => Set<Domain.Models.Session>();
    public DbSet<DomainEventOutbox> DomainEventOutbox => Set<DomainEventOutbox>();
    public DbSet<SessionShare> SessionShares => Set<SessionShare>();
    public DbSet<SessionPublicLink> SessionPublicLinks => Set<SessionPublicLink>();
    public DbSet<SessionShareAudit> SessionShareAudits => Set<SessionShareAudit>();
    public DbSet<MonthlyFocusSummary> MonthlyFocusSummaries => Set<MonthlyFocusSummary>();
    public DbSet<UserStatus> UserStatuses => Set<UserStatus>();
    public DbSet<UserStatusAudit> UserStatusAudits => Set<UserStatusAudit>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Domain.Models.Session>(e =>
        {
            e.Property(s => s.DurationMin).HasPrecision(5, 2);
        });

        modelBuilder.Entity<DomainEventOutbox>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedOnAdd();
            e.Property(x => x.EventType).HasMaxLength(256);
            e.Property(x => x.Payload).HasColumnType("text");
        });

        modelBuilder.Entity<SessionShare>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedOnAdd();
            e.HasIndex(x => new { x.SessionId, x.SharedWithUserId }).IsUnique();
        });

        modelBuilder.Entity<SessionPublicLink>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedOnAdd();
            e.HasIndex(x => x.Token).IsUnique();
            e.Property(x => x.Token).HasMaxLength(64);
        });

        modelBuilder.Entity<SessionShareAudit>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedOnAdd();
            e.Property(x => x.Action).HasMaxLength(64);
        });

        modelBuilder.Entity<MonthlyFocusSummary>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedOnAdd();
            e.Property(x => x.TotalDurationMin).HasPrecision(12, 2);
            e.HasIndex(x => new { x.UserId, x.Year, x.Month }).IsUnique();
        });

        modelBuilder.Entity<UserStatus>(e =>
        {
            e.HasKey(x => x.UserId);
            e.Property(x => x.Status).HasMaxLength(32);
        });

        modelBuilder.Entity<UserStatusAudit>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedOnAdd();
            e.Property(x => x.OldStatus).HasMaxLength(32);
            e.Property(x => x.NewStatus).HasMaxLength(32);
        });
    }
}
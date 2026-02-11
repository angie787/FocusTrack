using FocusTrack.RewardWorker.Events;
using FocusTrack.RewardWorker.Persistence;
using FocusTrack.RewardWorker.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace FocusTrack.RewardWorker.Tests;

public class DailyGoalServiceTests
{
    private static RewardsDbContext CreateInMemoryDb()
    {
        var options = new DbContextOptionsBuilder<RewardsDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new RewardsDbContext(options);
    }

    [Fact]
    public async Task OnSessionUpdated_WhenTotalIs119_99_DoesNotAwardBadge()
    {
        var db = CreateInMemoryDb();
        db.Database.EnsureCreated();
        var sessionApi = new Mock<ISessionApiClient>();
        var publisher = new Mock<IDailyGoalEventPublisher>();
        var service = new DailyGoalService(db, sessionApi.Object, publisher.Object, NullLogger<DailyGoalService>.Instance);

        var userId = "user-1";
        var date = new DateTimeOffset(2025, 4, 11, 0, 0, 0, TimeSpan.Zero);
        await service.OnSessionUpdatedAsync(new SessionUpdatedEventDto(
            Guid.NewGuid(), userId, "Topic", date.AddHours(1), date, 59.99m), default);
        await service.OnSessionUpdatedAsync(new SessionUpdatedEventDto(
            Guid.NewGuid(), userId, "Topic", date.AddHours(2), date.AddMinutes(60), 60.00m), default);
        // Total = 119.99

        sessionApi.Verify(x => x.SetDailyGoalAchievedAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        publisher.Verify(x => x.PublishAsync(It.IsAny<DailyGoalAchievedEvent>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task OnSessionUpdated_WhenTotalReaches120_00_AwardsBadge()
    {
        var db = CreateInMemoryDb();
        db.Database.EnsureCreated();
        var sessionApi = new Mock<ISessionApiClient>();
        sessionApi.Setup(x => x.SetDailyGoalAchievedAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var publisher = new Mock<IDailyGoalEventPublisher>();
        var service = new DailyGoalService(db, sessionApi.Object, publisher.Object, NullLogger<DailyGoalService>.Instance);

        var userId = "user-1";
        var date = new DateTimeOffset(2025, 4, 11, 0, 0, 0, TimeSpan.Zero);
        var triggeringSessionId = Guid.NewGuid();
        await service.OnSessionUpdatedAsync(new SessionUpdatedEventDto(
            Guid.NewGuid(), userId, "Topic", date.AddHours(1), date, 60.00m), default);
        await service.OnSessionUpdatedAsync(new SessionUpdatedEventDto(
            triggeringSessionId, userId, "Topic", date.AddHours(2), date.AddMinutes(60), 60.00m), default);
        // Total = 120.00, second session triggers

        sessionApi.Verify(x => x.SetDailyGoalAchievedAsync(triggeringSessionId, It.IsAny<CancellationToken>()), Times.Once);
        publisher.Verify(x => x.PublishAsync(It.Is<DailyGoalAchievedEvent>(e => e.SessionId == triggeringSessionId && e.UserId == userId), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task OnSessionUpdated_WhenTotalExceeds120_01_DoesNotAwardDuplicateBadge()
    {
        var db = CreateInMemoryDb();
        db.Database.EnsureCreated();
        var sessionApi = new Mock<ISessionApiClient>();
        sessionApi.Setup(x => x.SetDailyGoalAchievedAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var publisher = new Mock<IDailyGoalEventPublisher>();
        var service = new DailyGoalService(db, sessionApi.Object, publisher.Object, NullLogger<DailyGoalService>.Instance);

        var userId = "user-1";
        var date = new DateTimeOffset(2025, 4, 11, 0, 0, 0, TimeSpan.Zero);
        var firstSessionId = Guid.NewGuid();
        var secondSessionId = Guid.NewGuid();
        await service.OnSessionUpdatedAsync(new SessionUpdatedEventDto(
            firstSessionId, userId, "Topic", date.AddHours(1), date, 60.00m), default);
        await service.OnSessionUpdatedAsync(new SessionUpdatedEventDto(
            secondSessionId, userId, "Topic", date.AddHours(2), date.AddMinutes(60), 60.00m), default);
        // Total = 120.00 -> badge awarded for secondSessionId
        await service.OnSessionUpdatedAsync(new SessionUpdatedEventDto(
            Guid.NewGuid(), userId, "Topic", date.AddHours(3), date.AddMinutes(120), 0.01m), default);
        // Total = 120.01 -> no second badge

        sessionApi.Verify(x => x.SetDailyGoalAchievedAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Once);
        publisher.Verify(x => x.PublishAsync(It.IsAny<DailyGoalAchievedEvent>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}

using FocusTrack.Session.Infrastructure.Messaging;
using FocusTrack.Session.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FocusTrack.Session.Api.Outbox;

/// Polls the DomainEventOutbox table and publishes stored events to RabbitMQ asynchronously
public class OutboxProcessor : BackgroundService
{
    private const int BatchSize = 50;
    private const int PollIntervalMs = 1000;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OutboxProcessor> _logger;

    public OutboxProcessor(IServiceScopeFactory scopeFactory, ILogger<OutboxProcessor> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessBatchAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OutboxProcessor error");
            }

            await Task.Delay(PollIntervalMs, stoppingToken);
        }
    }

    private async Task ProcessBatchAsync(CancellationToken ct)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SessionDbContext>();
        var publisher = scope.ServiceProvider.GetRequiredService<IOutboxMessagePublisher>();

        var pending = await db.DomainEventOutbox
            .Where(x => x.ProcessedAt == null)
            .OrderBy(x => x.CreatedAt)
            .Take(BatchSize)
            .ToListAsync(ct);

        foreach (var row in pending)
        {
            try
            {
                await publisher.PublishAsync(row.EventType, row.Payload, ct);
                row.ProcessedAt = DateTimeOffset.UtcNow;
                _logger.LogInformation("Published outbox event {EventType} (Id {Id}) to message broker", row.EventType, row.Id);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to publish outbox event {Id} ({EventType}), will retry", row.Id, row.EventType);
                break;
            }
        }

        if (pending.Count > 0)
            await db.SaveChangesAsync(ct);
    }
}

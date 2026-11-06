using Microsoft.EntityFrameworkCore;
using MTC_System.Data;

namespace MTC_System.Realtime;

public sealed class BroadcastSchedulerHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<BroadcastSchedulerHostedService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromSeconds(30);

    public BroadcastSchedulerHostedService(
        IServiceScopeFactory scopeFactory,
        ILogger<BroadcastSchedulerHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("BroadcastSchedulerHostedService started.");

        using var timer = new PeriodicTimer(_checkInterval);
        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await CheckAndDispatchActiveBroadcastsAsync(stoppingToken);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogError(ex, "Error occurred during broadcast scheduling check.");
            }
        }
    }

    public async Task<int> CheckAndDispatchActiveBroadcastsAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MyDbContext>();
        var dispatcher = scope.ServiceProvider.GetRequiredService<IBroadcastRealtimeDispatcher>();

        var now = DateTimeOffset.UtcNow;

        // Find broadcasts that are approved ("Đã duyệt" or "Đang phát") and have active schedule
        var activeBroadcasts = await context.Broadcasts
            .AsNoTracking()
            .Where(b => !b.IsDeleted && (b.Status == "Đã duyệt" || b.Status == "Đang phát"))
            .Where(b => b.Schedules.Any(s => !s.IsDeleted &&
                                             s.StartDate <= now &&
                                             s.EndDate >= now))
            .ToListAsync(cancellationToken);

        int count = 0;
        foreach (var b in activeBroadcasts)
        {
            var dispatched = await dispatcher.DispatchBroadcastToDevicesAsync(b.Id, cancellationToken);
            if (dispatched > 0)
                count++;
        }

        return count;
    }
}

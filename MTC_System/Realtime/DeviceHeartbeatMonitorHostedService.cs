using Microsoft.EntityFrameworkCore;
using MTC_System.Data;
using MTC_System.Models;

namespace MTC_System.Realtime;

public sealed class DeviceHeartbeatMonitorHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DeviceHeartbeatMonitorHostedService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromSeconds(15);
    private readonly int _offlineThresholdSeconds;

    public DeviceHeartbeatMonitorHostedService(
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        ILogger<DeviceHeartbeatMonitorHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _offlineThresholdSeconds = configuration.GetValue("DeviceSettings:OfflineThresholdSeconds", 90);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("DeviceHeartbeatMonitorHostedService started with offline grace period of {Seconds}s.", _offlineThresholdSeconds);

        using var timer = new PeriodicTimer(_checkInterval);
        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await CheckTimeoutsAsync(stoppingToken);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogError(ex, "Error occurred during device heartbeat timeout check.");
            }
        }
    }

    public async Task<int> CheckTimeoutsAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MyDbContext>();

        var threshold = DateTimeOffset.UtcNow.AddSeconds(-_offlineThresholdSeconds);
        var timedOutDevices = await context.Devices
            .Where(x => !x.IsDeleted &&
                        x.ConnectionStatus == "Đang kết nối" &&
                        (x.LastOnlineAt == null || x.LastOnlineAt < threshold))
            .ToListAsync(cancellationToken);

        if (timedOutDevices.Count == 0)
            return 0;

        foreach (var device in timedOutDevices)
        {
            device.ConnectionStatus = "Mất kết nối";
            device.UpdatedAt = DateTimeOffset.UtcNow;

            context.DeviceActivityLogs.Add(new DeviceActivityLog
            {
                DeviceId = device.Id,
                ActionType = "HeartbeatTimeout",
                Result = "Mất kết nối",
                ActionDetail = $"{{\"reason\":\"Offline grace period expired (> {_offlineThresholdSeconds}s)\",\"lastOnlineAt\":\"{device.LastOnlineAt:O}\"}}"
            });
        }

        await context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Marked {Count} device(s) offline due to heartbeat timeout.", timedOutDevices.Count);
        return timedOutDevices.Count;
    }
}

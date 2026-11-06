using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using MTC_System.Data;
using MTC_System.Hubs;
using MTC_System.Models;

namespace MTC_System.Realtime;

public interface IBroadcastRealtimeDispatcher
{
    Task<int> DispatchBroadcastToDevicesAsync(Guid broadcastId, CancellationToken cancellationToken);
}

public sealed class BroadcastRealtimeDispatcher : IBroadcastRealtimeDispatcher
{
    private readonly MyDbContext _context;
    private readonly IHubContext<DeviceHub> _hubContext;
    private readonly IDeviceConnectionManager _connectionManager;
    private readonly IDeviceCommandManager _commandManager;
    private readonly ILogger<BroadcastRealtimeDispatcher> _logger;

    public BroadcastRealtimeDispatcher(
        MyDbContext context,
        IHubContext<DeviceHub> hubContext,
        IDeviceConnectionManager connectionManager,
        IDeviceCommandManager commandManager,
        ILogger<BroadcastRealtimeDispatcher> logger)
    {
        _context = context;
        _hubContext = hubContext;
        _connectionManager = connectionManager;
        _commandManager = commandManager;
        _logger = logger;
    }

    public async Task<int> DispatchBroadcastToDevicesAsync(Guid broadcastId, CancellationToken cancellationToken)
    {
        var broadcast = await _context.Broadcasts
            .Include(b => b.Schedules)
                .ThenInclude(s => s.BroadcastDevices)
            .FirstOrDefaultAsync(b => b.Id == broadcastId && !b.IsDeleted, cancellationToken);

        if (broadcast is null)
        {
            _logger.LogWarning("Broadcast {BroadcastId} not found.", broadcastId);
            return 0;
        }

        // Target devices: explicit devices in schedules, or all active devices in the broadcast facility
        var targetDeviceIds = broadcast.Schedules
            .SelectMany(s => s.BroadcastDevices)
            .Where(bd => !bd.IsDeleted)
            .Select(bd => bd.DeviceId)
            .Distinct()
            .ToList();

        if (targetDeviceIds.Count == 0)
        {
            targetDeviceIds = await _context.Devices
                .Where(d => d.FacilityId == broadcast.FacilityId && d.ActiveStatus && !d.IsDeleted)
                .Select(d => d.Id)
                .ToListAsync(cancellationToken);
        }

        if (targetDeviceIds.Count == 0)
        {
            _logger.LogInformation("Broadcast {BroadcastId} has no eligible devices in facility {FacilityId}.", broadcastId, broadcast.FacilityId);
            return 0;
        }

        int dispatchedCount = 0;
        var now = DateTimeOffset.UtcNow;

        foreach (var deviceId in targetDeviceIds)
        {
            var commandId = Guid.NewGuid();
            _commandManager.RegisterCommand(commandId, deviceId, "StartBroadcast", "Pending");

            bool isOnline = _connectionManager.TryGetConnectionId(deviceId, out var connectionId) && !string.IsNullOrEmpty(connectionId);

            var envelope = new DeviceCommandEnvelope
            {
                CommandId = commandId,
                DeviceId = deviceId,
                Type = "StartBroadcast",
                Payload = new
                {
                    broadcastId = broadcast.Id,
                    facilityId = broadcast.FacilityId,
                    sentAt = now
                }
            };

            if (isOnline)
            {
                await _hubContext.Clients.Client(connectionId!).SendAsync("ReceiveCommand", envelope, cancellationToken);
                _commandManager.MarkSent(commandId);
                dispatchedCount++;
            }

            // Ghi nhận DevicePlayHistory trạng thái ban đầu
            _context.DevicePlayHistories.Add(new DevicePlayHistory
            {
                DeviceId = deviceId,
                BroadcastId = broadcast.Id,
                StartedAt = now.UtcDateTime,
                PlayResult = isOnline ? "Đang phát" : "Thiết bị offline",
                Notes = isOnline ? $"Command {commandId} sent realtime via SignalR" : "Device offline when broadcast scheduled"
            });
        }

        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Dispatched broadcast {BroadcastId} to {Count} online device(s) out of {Total} target(s).",
            broadcastId, dispatchedCount, targetDeviceIds.Count);

        return dispatchedCount;
    }
}

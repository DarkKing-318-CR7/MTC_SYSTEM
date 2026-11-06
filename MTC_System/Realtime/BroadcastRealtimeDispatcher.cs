using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using MTC_System.Data;
using MTC_System.Hubs;
using MTC_System.Models;

namespace MTC_System.Realtime;

public interface IBroadcastRealtimeDispatcher
{
    Task<int> DispatchBroadcastToDevicesAsync(
        Guid broadcastId,
        Guid? scheduleId = null,
        Guid? timeSlotId = null,
        DateTimeOffset? occurrenceTime = null,
        CancellationToken cancellationToken = default);
}

public sealed class BroadcastRealtimeDispatcher : IBroadcastRealtimeDispatcher
{
    private readonly MyDbContext _context;
    private readonly IHubContext<DeviceHub> _hubContext;
    private readonly IDeviceConnectionManager _connectionManager;
    private readonly IDeviceCommandManager _commandManager;
    private readonly IDispatchOccurrenceManager _occurrenceManager;
    private readonly ILogger<BroadcastRealtimeDispatcher> _logger;

    public BroadcastRealtimeDispatcher(
        MyDbContext context,
        IHubContext<DeviceHub> hubContext,
        IDeviceConnectionManager connectionManager,
        IDeviceCommandManager commandManager,
        IDispatchOccurrenceManager occurrenceManager,
        ILogger<BroadcastRealtimeDispatcher> logger)
    {
        _context = context;
        _hubContext = hubContext;
        _connectionManager = connectionManager;
        _commandManager = commandManager;
        _occurrenceManager = occurrenceManager;
        _logger = logger;
    }

    public async Task<int> DispatchBroadcastToDevicesAsync(
        Guid broadcastId,
        Guid? scheduleId = null,
        Guid? timeSlotId = null,
        DateTimeOffset? occurrenceTime = null,
        CancellationToken cancellationToken = default)
    {
        var broadcast = await _context.Broadcasts
            .Include(b => b.Schedules)
                .ThenInclude(s => s.BroadcastDevices)
            .Include(b => b.Schedules)
                .ThenInclude(s => s.TimeSlots)
            .FirstOrDefaultAsync(b => b.Id == broadcastId && !b.IsDeleted, cancellationToken);

        if (broadcast is null)
        {
            _logger.LogWarning("Broadcast {BroadcastId} not found.", broadcastId);
            return 0;
        }

        var now = occurrenceTime ?? DateTimeOffset.UtcNow;

        // Target devices: explicit devices in schedules, or all active devices in the broadcast facility
        var targetDeviceIds = broadcast.Schedules
            .Where(s => !scheduleId.HasValue || s.Id == scheduleId.Value)
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
        var dateKey = now.UtcDateTime.ToString("yyyyMMdd");
        var activeScheduleId = scheduleId ?? broadcast.Schedules.FirstOrDefault()?.Id ?? Guid.Empty;
        var activeTimeSlotId = timeSlotId ?? broadcast.Schedules.SelectMany(s => s.TimeSlots).FirstOrDefault()?.Id ?? Guid.Empty;

        foreach (var deviceId in targetDeviceIds)
        {
            // Idempotency Occurrence Key: unique per broadcast, schedule, timeslot, date, and device
            var occurrenceKey = $"OCC:{broadcast.Id}:{activeScheduleId}:{activeTimeSlotId}:{dateKey}:{deviceId}";

            // 1. In-memory thread-safe idempotency check
            if (!_occurrenceManager.TryClaim(occurrenceKey))
            {
                _logger.LogInformation("Occurrence {OccurrenceKey} was already claimed in-memory. Skipping duplicate dispatch.", occurrenceKey);
                continue;
            }

            // 2. Persistent database check (in case of server restart)
            var alreadyDispatchedInDb = await _context.DevicePlayHistories
                .AnyAsync(h => h.BroadcastId == broadcast.Id &&
                               h.DeviceId == deviceId &&
                               h.Notes != null &&
                               h.Notes.Contains(occurrenceKey),
                          cancellationToken);

            if (alreadyDispatchedInDb)
            {
                _logger.LogInformation("Occurrence {OccurrenceKey} was already recorded in database. Skipping duplicate dispatch.", occurrenceKey);
                continue;
            }

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
                    scheduleId = activeScheduleId,
                    timeSlotId = activeTimeSlotId,
                    facilityId = broadcast.FacilityId,
                    occurrenceKey = occurrenceKey,
                    sentAt = now
                }
            };

            if (isOnline)
            {
                await _hubContext.Clients.Client(connectionId!).SendAsync("ReceiveCommand", envelope, cancellationToken);
                _commandManager.MarkSent(commandId);
                dispatchedCount++;
            }

            // Ghi nhận DevicePlayHistory trạng thái ban đầu gắn kèm OccurrenceKey để bảo toàn Idempotency
            _context.DevicePlayHistories.Add(new DevicePlayHistory
            {
                DeviceId = deviceId,
                BroadcastId = broadcast.Id,
                BroadcastScheduleId = activeScheduleId != Guid.Empty ? activeScheduleId : null,
                StartedAt = now.UtcDateTime,
                PlayResult = isOnline ? "Đang phát" : "Thiết bị offline",
                Notes = isOnline
                    ? $"OccurrenceKey={occurrenceKey}; CommandId={commandId}; Status=SentRealtime"
                    : $"OccurrenceKey={occurrenceKey}; CommandId={commandId}; Status=DeviceOffline"
            });
        }

        if (dispatchedCount > 0 || _context.ChangeTracker.HasChanges())
        {
            await _context.SaveChangesAsync(cancellationToken);
        }

        _logger.LogInformation("Dispatched broadcast {BroadcastId} to {Count} online device(s) out of {Total} target(s).",
            broadcastId, dispatchedCount, targetDeviceIds.Count);

        return dispatchedCount;
    }
}

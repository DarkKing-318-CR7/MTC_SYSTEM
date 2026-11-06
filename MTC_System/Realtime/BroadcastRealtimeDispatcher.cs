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

        DateTimeOffset now;
        if (occurrenceTime.HasValue)
        {
            now = occurrenceTime.Value;
        }
        else
        {
            var tzSetting = await _context.SystemSettings
                .AsNoTracking()
                .Where(s => s.OrganizationId == broadcast.OrganizationId && !s.IsDeleted && s.SettingKey == "TIMEZONE")
                .Select(s => s.SettingValue)
                .FirstOrDefaultAsync(cancellationToken);
            var orgTz = ResolveTimeZone(tzSetting);
            now = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, orgTz);
        }

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
        // P1 Fix: Định dạng dateKey theo giờ địa phương (local offset) của occurrence, tuyệt đối không gọi UtcDateTime
        var dateKey = now.ToString("yyyyMMdd");
        var activeScheduleId = scheduleId ?? broadcast.Schedules.FirstOrDefault()?.Id ?? Guid.Empty;
        var activeTimeSlotId = timeSlotId ?? broadcast.Schedules.SelectMany(s => s.TimeSlots).FirstOrDefault()?.Id ?? Guid.Empty;

        // P1 Fix: Trích xuất thông tin nội dung Media / Playlist thật theo DEVICE_REALTIME_CONTRACT.md
        Media? media = null;
        int durationSeconds = 0;

        if (broadcast.MediaId.HasValue)
        {
            media = await _context.Media.AsNoTracking().FirstOrDefaultAsync(m => m.Id == broadcast.MediaId.Value, cancellationToken);
            if (media?.DurationSeconds > 0)
                durationSeconds = media.DurationSeconds;
        }
        else if (broadcast.PlaylistId.HasValue)
        {
            var playlist = await _context.Playlists
                .AsNoTracking()
                .Include(p => p.Items)
                .FirstOrDefaultAsync(p => p.Id == broadcast.PlaylistId.Value && !p.IsDeleted, cancellationToken);

            if (playlist != null && playlist.Items.Count > 0)
            {
                var firstItem = playlist.Items.Where(i => !i.IsDeleted).OrderBy(i => i.DisplayOrder).FirstOrDefault();
                if (firstItem != null)
                {
                    media = await _context.Media.AsNoTracking().FirstOrDefaultAsync(m => m.Id == firstItem.MediaId && !m.IsDeleted, cancellationToken);
                    durationSeconds = playlist.TotalDurationSeconds > 0 ? playlist.TotalDurationSeconds : (firstItem.DurationSeconds > 0 ? firstItem.DurationSeconds : 30);
                }
            }
        }

        string mediaUrl;
        if (media != null)
        {
            if (!string.IsNullOrWhiteSpace(media.Url))
            {
                mediaUrl = media.Url;
            }
            else
            {
                mediaUrl = $"/api/media/{media.Id}/stream";
            }

            if (durationSeconds <= 0)
            {
                durationSeconds = media.DurationSeconds > 0 ? media.DurationSeconds : 30;
            }
        }
        else
        {
            mediaUrl = $"/api/broadcasts/{broadcast.Id}/content";
            durationSeconds = 30;
        }

        foreach (var deviceId in targetDeviceIds)
        {
            // Idempotency Occurrence Key: unique per broadcast, schedule, timeslot, date, and device
            var occurrenceKey = $"OCC:{broadcast.Id}:{activeScheduleId}:{activeTimeSlotId}:{dateKey}:{deviceId}";

            // 1. Kiểm tra CSDL xem occurrence này đã được điều phối thành công ("Đang phát" hoặc "Hoàn thành") hay chưa
            var alreadyDispatchedInDb = await _context.DevicePlayHistories
                .AnyAsync(h => h.BroadcastId == broadcast.Id &&
                               h.DeviceId == deviceId &&
                               (h.PlayResult == "Đang phát" || h.PlayResult == "Hoàn thành") &&
                               h.Notes != null &&
                               h.Notes.Contains(occurrenceKey),
                          cancellationToken);

            if (alreadyDispatchedInDb)
            {
                _logger.LogInformation("Occurrence {OccurrenceKey} was already successfully dispatched/completed in database. Skipping duplicate dispatch.", occurrenceKey);
                continue;
            }

            bool isOnline = _connectionManager.TryGetConnectionId(deviceId, out var connectionId) && !string.IsNullOrEmpty(connectionId);

            if (!isOnline)
            {
                // P1 – Thiết bị đang offline:
                // Ghi nhận trạng thái "Thiết bị offline" vào CSDL nếu chưa có để phục vụ audit/báo cáo,
                // TUYỆT ĐỐI KHÔNG khóa vĩnh viễn occurrence để khi thiết bị online lại trong khung giờ, scheduler sẽ khôi phục và phát lại!
                var offlineHistoryExists = await _context.DevicePlayHistories
                    .AnyAsync(h => h.BroadcastId == broadcast.Id &&
                                   h.DeviceId == deviceId &&
                                   h.PlayResult == "Thiết bị offline" &&
                                   h.Notes != null &&
                                   h.Notes.Contains(occurrenceKey),
                              cancellationToken);

                if (!offlineHistoryExists)
                {
                    _context.DevicePlayHistories.Add(new DevicePlayHistory
                    {
                        DeviceId = deviceId,
                        BroadcastId = broadcast.Id,
                        BroadcastScheduleId = activeScheduleId != Guid.Empty ? activeScheduleId : null,
                        StartedAt = now.UtcDateTime,
                        PlayResult = "Thiết bị offline",
                        Notes = $"OccurrenceKey={occurrenceKey}; Status=DeviceOffline"
                    });
                }

                _logger.LogInformation("Device {DeviceId} is offline for occurrence {OccurrenceKey}. Saved offline state and retained turn for slot recovery.", deviceId, occurrenceKey);
                continue;
            }

            // Thiết bị đang online: Thực hiện chiếm khóa nguyên tử in-memory
            if (!_occurrenceManager.TryClaim(occurrenceKey))
            {
                _logger.LogInformation("Occurrence {OccurrenceKey} was already claimed in-memory. Skipping duplicate dispatch.", occurrenceKey);
                continue;
            }

            var commandId = Guid.NewGuid();
            _commandManager.RegisterCommand(commandId, deviceId, "StartBroadcast", "Pending");

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
                    broadcastCode = broadcast.BroadcastCode,
                    broadcastName = broadcast.BroadcastName,
                    mediaUrl = mediaUrl,
                    durationSeconds = durationSeconds,
                    checksum = media?.ChecksumSha256 ?? media?.ChecksumMd5,
                    occurrenceKey = occurrenceKey,
                    sentAt = now
                }
            };

            // P1 Fix: Bọc try-catch quanh SendAsync; nếu gửi thất bại thì giải phóng khóa để scheduler có thể thử lại
            try
            {
                await _hubContext.Clients.Client(connectionId!).SendAsync("ReceiveCommand", envelope, cancellationToken);
                _commandManager.MarkSent(commandId);
                dispatchedCount++;

                // Nếu trước đó đã ghi nhận "Thiết bị offline" cho occurrence này, nâng cấp bản ghi sang "Đang phát"
                var existingOfflineHistory = await _context.DevicePlayHistories
                    .FirstOrDefaultAsync(h => h.BroadcastId == broadcast.Id &&
                                              h.DeviceId == deviceId &&
                                              h.PlayResult == "Thiết bị offline" &&
                                              h.Notes != null &&
                                              h.Notes.Contains(occurrenceKey),
                                         cancellationToken);

                if (existingOfflineHistory != null)
                {
                    existingOfflineHistory.StartedAt = now.UtcDateTime;
                    existingOfflineHistory.PlayResult = "Đang phát";
                    existingOfflineHistory.Notes = $"OccurrenceKey={occurrenceKey}; CommandId={commandId}; Status=SentRealtime(RecoveredOnline)";
                }
                else
                {
                    _context.DevicePlayHistories.Add(new DevicePlayHistory
                    {
                        DeviceId = deviceId,
                        BroadcastId = broadcast.Id,
                        BroadcastScheduleId = activeScheduleId != Guid.Empty ? activeScheduleId : null,
                        StartedAt = now.UtcDateTime,
                        PlayResult = "Đang phát",
                        Notes = $"OccurrenceKey={occurrenceKey}; CommandId={commandId}; Status=SentRealtime"
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send command to device {DeviceId} via SignalR. Releasing occurrence claim {OccurrenceKey}.", deviceId, occurrenceKey);
                _occurrenceManager.Release(occurrenceKey);
                _commandManager.ProcessAck(commandId, deviceId, "Failed", ex.Message);
            }
        }

        if (dispatchedCount > 0 || _context.ChangeTracker.HasChanges())
        {
            await _context.SaveChangesAsync(cancellationToken);
        }

        _logger.LogInformation("Dispatched broadcast {BroadcastId} to {Count} online device(s) out of {Total} target(s).",
            broadcastId, dispatchedCount, targetDeviceIds.Count);

        return dispatchedCount;
    }

    private static TimeZoneInfo ResolveTimeZone(string? tzString)
    {
        if (string.IsNullOrWhiteSpace(tzString))
            tzString = "SE Asia Standard Time";

        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(tzString);
        }
        catch (TimeZoneNotFoundException)
        {
            try
            {
                if (tzString.Equals("SE Asia Standard Time", StringComparison.OrdinalIgnoreCase) ||
                    tzString.Contains("07:00") || tzString.Contains("Bangkok") || tzString.Contains("Hanoi") || tzString.Contains("Vietnam"))
                {
                    return TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh");
                }
                return TimeZoneInfo.CreateCustomTimeZone("UTC+07:00", TimeSpan.FromHours(7), "SE Asia Standard Time", "SE Asia Standard Time");
            }
            catch
            {
                return TimeZoneInfo.CreateCustomTimeZone("UTC+07:00", TimeSpan.FromHours(7), "SE Asia Standard Time", "SE Asia Standard Time");
            }
        }
    }
}

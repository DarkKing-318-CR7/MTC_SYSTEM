using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using MTC_System.Data;
using MTC_System.Models;
using MTC_System.Realtime;

namespace MTC_System.Hubs;

[Authorize(AuthenticationSchemes = "DeviceBearer")]
public sealed class DeviceHub : Hub
{
    private readonly MyDbContext _context;
    private readonly IDeviceConnectionManager _connectionManager;
    private readonly IDeviceCommandManager _commandManager;
    private readonly ILogger<DeviceHub> _logger;

    public DeviceHub(
        MyDbContext context,
        IDeviceConnectionManager connectionManager,
        IDeviceCommandManager commandManager,
        ILogger<DeviceHub> logger)
    {
        _context = context;
        _connectionManager = connectionManager;
        _commandManager = commandManager;
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        var deviceId = GetDeviceId();
        if (deviceId == Guid.Empty)
        {
            Context.Abort();
            return;
        }

        var device = await _context.Devices.FirstOrDefaultAsync(x => x.Id == deviceId && !x.IsDeleted);
        if (device is null || !device.ActiveStatus || !device.IsActivated)
        {
            _logger.LogWarning("Device {DeviceId} connection rejected because it is locked, inactive, or not found.", deviceId);
            Context.Abort();
            return;
        }

        _connectionManager.RegisterConnection(deviceId, Context.ConnectionId);

        device.ConnectionStatus = "Đang kết nối";
        device.LastOnlineAt = DateTimeOffset.UtcNow;
        device.UpdatedAt = DateTimeOffset.UtcNow;

        _context.DeviceActivityLogs.Add(new DeviceActivityLog
        {
            DeviceId = deviceId,
            ActionType = "DeviceConnected",
            Result = "Thành công",
            IpAddress = Context.GetHttpContext()?.Connection.RemoteIpAddress?.ToString(),
            ActionDetail = $"{{\"connectionId\":\"{Context.ConnectionId}\"}}"
        });

        await _context.SaveChangesAsync();
        await base.OnConnectedAsync();
        _logger.LogInformation("Device {DeviceId} connected successfully with connectionId {ConnectionId}.", deviceId, Context.ConnectionId);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var deviceId = GetDeviceId();
        _connectionManager.UnregisterConnection(Context.ConnectionId);

        // Fail-safe offline grace: Do NOT immediately flip status to "Mất kết nối" to allow short reconnects.
        // DeviceHeartbeatMonitorHostedService will mark offline if last heartbeat > 90 seconds.
        if (deviceId != Guid.Empty)
        {
            try
            {
                _context.DeviceActivityLogs.Add(new DeviceActivityLog
                {
                    DeviceId = deviceId,
                    ActionType = "DeviceTransportDisconnected",
                    Result = "Đang chờ grace period (90s)",
                    IpAddress = Context.GetHttpContext()?.Connection.RemoteIpAddress?.ToString(),
                    ActionDetail = exception?.Message
                });

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging transport disconnection for {DeviceId}", deviceId);
            }
        }

        await base.OnDisconnectedAsync(exception);
    }

    public async Task<bool> SendHeartbeat(DeviceHeartbeatRequest request)
    {
        var deviceId = GetDeviceId();
        if (deviceId == Guid.Empty) return false;

        var device = await _context.Devices.FirstOrDefaultAsync(x => x.Id == deviceId && !x.IsDeleted);
        if (device is null || !device.ActiveStatus) return false;

        device.ConnectionStatus = "Đang kết nối";
        device.LastOnlineAt = DateTimeOffset.UtcNow;
        if (request.Volume.HasValue && request.Volume.Value >= 0 && request.Volume.Value <= 100)
            device.Volume = request.Volume.Value;
        if (!string.IsNullOrWhiteSpace(request.PlayStatus))
            device.PlayStatus = request.PlayStatus.Trim();
        if (request.Latitude.HasValue)
            device.Latitude = request.Latitude.Value;
        if (request.Longitude.HasValue)
            device.Longitude = request.Longitude.Value;
        if (!string.IsNullOrWhiteSpace(request.IpAddress))
            device.IpAddress = request.IpAddress.Trim();
        device.UpdatedAt = DateTimeOffset.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> AcknowledgeCommand(DeviceCommandAckRequest request)
    {
        var deviceId = GetDeviceId();
        if (deviceId == Guid.Empty) return false;

        var ackResult = _commandManager.ProcessAck(request.CommandId, deviceId, request.Status, request.ErrorMessage);

        if (ackResult == CommandAckResult.NotFound)
        {
            _logger.LogWarning("Command {CommandId} not found during ACK from device {DeviceId}.", request.CommandId, deviceId);
            return false;
        }

        if (ackResult == CommandAckResult.MismatchDevice)
        {
            _logger.LogWarning("Device {DeviceId} attempted to ACK command {CommandId} belonging to another device.", deviceId, request.CommandId);
            return false;
        }

        if (ackResult == CommandAckResult.Duplicate)
        {
            _logger.LogInformation("Duplicate ACK for command {CommandId} from device {DeviceId} ignored idempotently.", request.CommandId, deviceId);
            return true;
        }

        // Apply volume ONLY after successful ACK! (Item 8)
        var cmdState = _commandManager.GetCommand(request.CommandId);
        if (cmdState != null && cmdState.CommandType == "SetVolume" && request.Status.Equals("Success", StringComparison.OrdinalIgnoreCase) && cmdState.TargetVolume.HasValue)
        {
            var device = await _context.Devices.FirstOrDefaultAsync(x => x.Id == deviceId && !x.IsDeleted);
            if (device != null)
            {
                device.Volume = cmdState.TargetVolume.Value;
                device.UpdatedAt = DateTimeOffset.UtcNow;
            }
        }

        _context.DeviceActivityLogs.Add(new DeviceActivityLog
        {
            DeviceId = deviceId,
            ActionType = "CommandAck",
            Result = request.Status,
            ErrorMessage = request.ErrorMessage,
            ActionDetail = $"{{\"commandId\":\"{request.CommandId}\",\"executedAt\":\"{request.ExecutedAt:O}\"}}"
        });

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ReportPlaybackResult(PlaybackReportRequest request)
    {
        var deviceId = GetDeviceId();
        if (deviceId == Guid.Empty) return false;

        var device = await _context.Devices.FirstOrDefaultAsync(x => x.Id == deviceId && !x.IsDeleted);
        if (device == null || !device.ActiveStatus) return false;

        // P1 – Kiểm tra bản tin tồn tại và phạm vi dữ liệu thiết bị/cơ sở
        var broadcast = await _context.Broadcasts
            .Include(b => b.Schedules)
                .ThenInclude(s => s.BroadcastDevices)
            .FirstOrDefaultAsync(b => b.Id == request.BroadcastId && !b.IsDeleted);

        if (broadcast == null) return false;

        bool isSameFacility = broadcast.FacilityId == device.FacilityId && broadcast.OrganizationId == device.OrganizationId;
        bool isTargetedDevice = broadcast.Schedules.Any(s => !s.IsDeleted && s.BroadcastDevices.Any(bd => !bd.IsDeleted && bd.DeviceId == deviceId));
        if (!isSameFacility && !isTargetedDevice)
        {
            return false;
        }

        // P1 – Hoàn tất lịch sử "Đang phát" hiện có thay vì tạo thêm bản ghi trùng lặp
        var existingHistory = await _context.DevicePlayHistories
            .Where(h => h.DeviceId == deviceId && h.BroadcastId == request.BroadcastId && h.PlayResult == "Đang phát")
            .OrderByDescending(h => h.StartedAt)
            .FirstOrDefaultAsync();

        if (string.IsNullOrWhiteSpace(request.Result))
        {
            _logger.LogWarning("Rejecting playback report for broadcast {BroadcastId} because result is empty.", request.BroadcastId);
            return false;
        }

        var rawResult = request.Result.Trim();
        string normalizedResult;
        if (string.Equals(rawResult, "Hoàn thành", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(rawResult, "Thành công", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(rawResult, "Success", StringComparison.OrdinalIgnoreCase))
        {
            normalizedResult = "Hoàn thành";
        }
        else if (string.Equals(rawResult, "Thất bại", StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(rawResult, "Failed", StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(rawResult, "Error", StringComparison.OrdinalIgnoreCase))
        {
            normalizedResult = "Thất bại";
        }
        else
        {
            _logger.LogWarning("Rejecting playback report for broadcast {BroadcastId} with invalid result '{Result}'.", request.BroadcastId, request.Result);
            return false;
        }

        if (existingHistory != null)
        {
            existingHistory.EndedAt = request.EndedAt.UtcDateTime;
            existingHistory.PlayResult = normalizedResult;
            existingHistory.Notes = string.IsNullOrWhiteSpace(existingHistory.Notes)
                ? request.Notes
                : $"{existingHistory.Notes}; Result={normalizedResult}; ReportNotes={request.Notes}";
        }
        else
        {
            // Kiểm tra xem phiên phát sóng này đã được báo cáo hoàn tất trước đó hay chưa (idempotent Report)
            var alreadyCompleted = await _context.DevicePlayHistories
                .AnyAsync(h => h.DeviceId == deviceId && h.BroadcastId == request.BroadcastId &&
                               (h.PlayResult == "Hoàn thành" || h.PlayResult == "Thành công" || h.PlayResult == "Thất bại"));

            if (alreadyCompleted)
            {
                return true;
            }

            // Không tìm thấy phiên phát sóng nào được dispatch tới thiết bị này -> Từ chối báo cáo giả
            return false;
        }

        device.PlayStatus = "Dừng phát bản tin";
        device.UpdatedAt = DateTimeOffset.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }

    private Guid GetDeviceId()
    {
        var claim = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(claim, out var id) ? id : Guid.Empty;
    }
}

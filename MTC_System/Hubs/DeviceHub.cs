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
    private readonly ILogger<DeviceHub> _logger;

    public DeviceHub(MyDbContext context, IDeviceConnectionManager connectionManager, ILogger<DeviceHub> logger)
    {
        _context = context;
        _connectionManager = connectionManager;
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

        if (deviceId != Guid.Empty)
        {
            try
            {
                var device = await _context.Devices.FirstOrDefaultAsync(x => x.Id == deviceId && !x.IsDeleted);
                if (device is not null)
                {
                    device.ConnectionStatus = "Mất kết nối";
                    device.UpdatedAt = DateTimeOffset.UtcNow;

                    _context.DeviceActivityLogs.Add(new DeviceActivityLog
                    {
                        DeviceId = deviceId,
                        ActionType = "DeviceDisconnected",
                        Result = "Thành công",
                        IpAddress = Context.GetHttpContext()?.Connection.RemoteIpAddress?.ToString(),
                        ActionDetail = exception?.Message
                    });

                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating device disconnection status for {DeviceId}", deviceId);
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

        _context.DevicePlayHistories.Add(new DevicePlayHistory
        {
            DeviceId = deviceId,
            BroadcastId = request.BroadcastId,
            StartedAt = request.StartedAt.UtcDateTime,
            EndedAt = request.EndedAt.UtcDateTime,
            PlayResult = request.Result,
            Notes = request.Notes
        });

        await _context.SaveChangesAsync();
        return true;
    }

    private Guid GetDeviceId()
    {
        var claim = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(claim, out var id) ? id : Guid.Empty;
    }
}

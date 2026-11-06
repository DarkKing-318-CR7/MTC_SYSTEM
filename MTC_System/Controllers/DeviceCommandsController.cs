using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using MTC_System.Common;
using MTC_System.Data;
using MTC_System.Hubs;
using MTC_System.Models;
using MTC_System.Realtime;

namespace MTC_System.Controllers;

[ApiController]
[Authorize(Policy = "DeviceControl")]
[Route("api/devices")]
public sealed class DeviceCommandsController : ControllerBase
{
    private readonly MyDbContext _context;
    private readonly IHubContext<DeviceHub> _hubContext;
    private readonly IDeviceConnectionManager _connectionManager;
    private readonly IDeviceCommandManager _commandManager;

    public DeviceCommandsController(
        MyDbContext context,
        IHubContext<DeviceHub> hubContext,
        IDeviceConnectionManager connectionManager,
        IDeviceCommandManager commandManager)
    {
        _context = context;
        _hubContext = hubContext;
        _connectionManager = connectionManager;
        _commandManager = commandManager;
    }

    [HttpPost("{id:guid}/commands/volume")]
    public async Task<ActionResult<ApiResponse<object>>> SetVolume(
        Guid id,
        [FromBody] SetVolumeCommandRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Volume < 0 || request.Volume > 100)
            return BadRequest(ApiResponse<object>.Fail("Âm lượng phải nằm trong khoảng 0 đến 100."));

        var device = await _context.Devices.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);
        if (device is null)
            return NotFound(ApiResponse<object>.Fail("Không tìm thấy thiết bị."));

        var scope = User.GetUserScope();
        scope.ValidateScope(device.OrganizationId, device.FacilityId);

        var commandId = Guid.NewGuid();
        _commandManager.RegisterCommand(commandId, device.Id, "SetVolume", "Pending");

        var envelope = new DeviceCommandEnvelope
        {
            CommandId = commandId,
            DeviceId = device.Id,
            Type = "SetVolume",
            Payload = new { volume = request.Volume }
        };

        bool sent = false;
        if (_connectionManager.TryGetConnectionId(device.Id, out var connectionId) && !string.IsNullOrEmpty(connectionId))
        {
            await _hubContext.Clients.Client(connectionId).SendAsync("ReceiveCommand", envelope, cancellationToken);
            _commandManager.MarkSent(commandId);
            sent = true;
        }

        device.Volume = request.Volume;
        device.UpdatedAt = DateTimeOffset.UtcNow;

        _context.DeviceActivityLogs.Add(new DeviceActivityLog
        {
            DeviceId = device.Id,
            UserId = scope.UserId,
            ActionType = "SetVolume",
            Result = sent ? "Đã gửi" : "Thiết bị offline",
            ActionDetail = $"{{\"volume\":{request.Volume},\"commandId\":\"{commandId}\",\"sentRealtime\":{sent.ToString().ToLower()}}}"
        });

        await _context.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<object>.Ok(new { commandId, sentRealtime = sent, volume = request.Volume }, "Lệnh điều chỉnh âm lượng đã được tạo."));
    }

    [HttpPost("{id:guid}/commands/reboot")]
    public async Task<ActionResult<ApiResponse<object>>> Reboot(
        Guid id,
        CancellationToken cancellationToken)
    {
        var device = await _context.Devices.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);
        if (device is null)
            return NotFound(ApiResponse<object>.Fail("Không tìm thấy thiết bị."));

        var scope = User.GetUserScope();
        scope.ValidateScope(device.OrganizationId, device.FacilityId);

        var commandId = Guid.NewGuid();
        _commandManager.RegisterCommand(commandId, device.Id, "Reboot", "Pending");

        var envelope = new DeviceCommandEnvelope
        {
            CommandId = commandId,
            DeviceId = device.Id,
            Type = "Reboot",
            Payload = new { delaySeconds = 5 }
        };

        bool sent = false;
        if (_connectionManager.TryGetConnectionId(device.Id, out var connectionId) && !string.IsNullOrEmpty(connectionId))
        {
            await _hubContext.Clients.Client(connectionId).SendAsync("ReceiveCommand", envelope, cancellationToken);
            _commandManager.MarkSent(commandId);
            sent = true;
        }

        _context.DeviceActivityLogs.Add(new DeviceActivityLog
        {
            DeviceId = device.Id,
            UserId = scope.UserId,
            ActionType = "Reboot",
            Result = sent ? "Đã gửi" : "Thiết bị offline",
            ActionDetail = $"{{\"commandId\":\"{commandId}\",\"sentRealtime\":{sent.ToString().ToLower()}}}"
        });

        await _context.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<object>.Ok(new { commandId, sentRealtime = sent }, "Lệnh khởi động lại đã được tạo."));
    }
}

public sealed class SetVolumeCommandRequest
{
    public int Volume { get; set; }
}

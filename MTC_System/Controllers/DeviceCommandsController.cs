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
[Authorize]
[Route("api/devices")]
public sealed class DeviceCommandsController : ControllerBase
{
    private readonly MyDbContext _context;
    private readonly IHubContext<DeviceHub> _hubContext;
    private readonly IDeviceConnectionManager _connectionManager;

    public DeviceCommandsController(
        MyDbContext context,
        IHubContext<DeviceHub> hubContext,
        IDeviceConnectionManager connectionManager)
    {
        _context = context;
        _hubContext = hubContext;
        _connectionManager = connectionManager;
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

        if (!EnsureUserScope(device))
            return Forbid();

        var commandId = Guid.NewGuid();
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
            sent = true;
        }

        device.Volume = request.Volume;
        device.UpdatedAt = DateTimeOffset.UtcNow;

        _context.DeviceActivityLogs.Add(new DeviceActivityLog
        {
            DeviceId = device.Id,
            UserId = GetUserId(),
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

        if (!EnsureUserScope(device))
            return Forbid();

        var commandId = Guid.NewGuid();
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
            sent = true;
        }

        _context.DeviceActivityLogs.Add(new DeviceActivityLog
        {
            DeviceId = device.Id,
            UserId = GetUserId(),
            ActionType = "Reboot",
            Result = sent ? "Đã gửi" : "Thiết bị offline",
            ActionDetail = $"{{\"commandId\":\"{commandId}\",\"sentRealtime\":{sent.ToString().ToLower()}}}"
        });

        await _context.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<object>.Ok(new { commandId, sentRealtime = sent }, "Lệnh khởi động lại đã được tạo."));
    }

    private bool EnsureUserScope(Device device)
    {
        if (User.IsInRole("ADMINISTRATOR") || User.IsInRole("Administrator"))
            return true;

        var orgClaim = User.FindFirstValue("organizationId");
        if (Guid.TryParse(orgClaim, out var userOrgId) && userOrgId != device.OrganizationId)
            return false;

        var facilityClaim = User.FindFirstValue("facilityId");
        if (Guid.TryParse(facilityClaim, out var userFacilityId) && userFacilityId != device.FacilityId)
            return false;

        return true;
    }

    private Guid? GetUserId() =>
        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;
}

public sealed class SetVolumeCommandRequest
{
    public int Volume { get; set; }
}

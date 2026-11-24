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

        var result = await SendVolumeAsync(device, request.Volume, scope.UserId, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<object>.Ok(result, "Lệnh điều chỉnh âm lượng đã được tạo."));
    }

    [HttpPost("commands/volume")]
    public async Task<ActionResult<ApiResponse<object>>> SetVolumeBulk(
        [FromBody] BulkSetVolumeCommandRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Volume < 0 || request.Volume > 100)
            return BadRequest(ApiResponse<object>.Fail("Âm lượng phải nằm trong khoảng 0 đến 100."));

        var validation = await LoadBulkDevicesAsync(request.DeviceIds, cancellationToken);
        if (validation.Error is not null)
            return BadRequest(ApiResponse<object>.Fail(validation.Error));

        var scope = User.GetUserScope();
        foreach (var device in validation.Devices)
            scope.ValidateScope(device.OrganizationId, device.FacilityId);

        var results = new List<DeviceCommandResult>();
        foreach (var device in validation.Devices)
            results.Add(await SendVolumeAsync(device, request.Volume, scope.UserId, cancellationToken));

        await _context.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<object>.Ok(new { count = results.Count, results }, "Đã tạo lệnh âm lượng hàng loạt."));
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

        var result = await SendRebootAsync(device, scope.UserId, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<object>.Ok(result, "Lệnh khởi động lại đã được tạo."));
    }

    [HttpPost("commands/reboot")]
    public async Task<ActionResult<ApiResponse<object>>> RebootBulk(
        [FromBody] BulkRebootCommandRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await LoadBulkDevicesAsync(request.DeviceIds, cancellationToken);
        if (validation.Error is not null)
            return BadRequest(ApiResponse<object>.Fail(validation.Error));

        var scope = User.GetUserScope();
        foreach (var device in validation.Devices)
            scope.ValidateScope(device.OrganizationId, device.FacilityId);

        var results = new List<DeviceCommandResult>();
        foreach (var device in validation.Devices)
            results.Add(await SendRebootAsync(device, scope.UserId, cancellationToken));

        await _context.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<object>.Ok(new { count = results.Count, results }, "Đã tạo lệnh khởi động lại hàng loạt."));
    }

    private async Task<(List<Device> Devices, string? Error)> LoadBulkDevicesAsync(
        IReadOnlyCollection<Guid>? requestedIds,
        CancellationToken cancellationToken)
    {
        var ids = requestedIds?.Where(x => x != Guid.Empty).Distinct().ToList() ?? [];
        if (ids.Count == 0 || ids.Count > 100)
            return ([], "Danh sách thiết bị phải có từ 1 đến 100 mã hợp lệ.");

        var devices = await _context.Devices
            .Where(x => ids.Contains(x.Id) && !x.IsDeleted)
            .ToListAsync(cancellationToken);
        if (devices.Count != ids.Count)
            return ([], "Có thiết bị không tồn tại hoặc đã bị xóa; không lệnh nào được tạo.");

        return (devices, null);
    }

    private async Task<DeviceCommandResult> SendVolumeAsync(
        Device device,
        int volume,
        Guid? userId,
        CancellationToken cancellationToken)
    {
        var commandId = Guid.NewGuid();
        _commandManager.RegisterCommand(commandId, device.Id, "SetVolume", "Pending", targetVolume: volume);
        var sent = await SendEnvelopeAsync(device.Id, commandId, "SetVolume", new { volume }, cancellationToken);

        _context.DeviceActivityLogs.Add(new DeviceActivityLog
        {
            DeviceId = device.Id,
            UserId = userId,
            ActionType = "SetVolume",
            Result = sent ? "Đã gửi" : "Thiết bị offline",
            ActionDetail = $"{{\"volume\":{volume},\"commandId\":\"{commandId}\",\"sentRealtime\":{sent.ToString().ToLower()}}}"
        });

        return new DeviceCommandResult(device.Id, commandId, sent, volume);
    }

    private async Task<DeviceCommandResult> SendRebootAsync(
        Device device,
        Guid? userId,
        CancellationToken cancellationToken)
    {
        var commandId = Guid.NewGuid();
        _commandManager.RegisterCommand(commandId, device.Id, "Reboot", "Pending");
        var sent = await SendEnvelopeAsync(device.Id, commandId, "Reboot", new { delaySeconds = 5 }, cancellationToken);

        _context.DeviceActivityLogs.Add(new DeviceActivityLog
        {
            DeviceId = device.Id,
            UserId = userId,
            ActionType = "Reboot",
            Result = sent ? "Đã gửi" : "Thiết bị offline",
            ActionDetail = $"{{\"commandId\":\"{commandId}\",\"sentRealtime\":{sent.ToString().ToLower()}}}"
        });

        return new DeviceCommandResult(device.Id, commandId, sent, null);
    }

    private async Task<bool> SendEnvelopeAsync(
        Guid deviceId,
        Guid commandId,
        string type,
        object payload,
        CancellationToken cancellationToken)
    {
        if (!_connectionManager.TryGetConnectionId(deviceId, out var connectionId) || string.IsNullOrEmpty(connectionId))
            return false;

        await _hubContext.Clients.Client(connectionId).SendAsync("ReceiveCommand", new DeviceCommandEnvelope
        {
            CommandId = commandId,
            DeviceId = deviceId,
            Type = type,
            Payload = payload
        }, cancellationToken);
        _commandManager.MarkSent(commandId);
        return true;
    }
}

public class SetVolumeCommandRequest
{
    public int Volume { get; set; }
}

public sealed class BulkSetVolumeCommandRequest : SetVolumeCommandRequest
{
    public List<Guid> DeviceIds { get; set; } = [];
}

public sealed class BulkRebootCommandRequest
{
    public List<Guid> DeviceIds { get; set; } = [];
}

public sealed record DeviceCommandResult(Guid DeviceId, Guid CommandId, bool SentRealtime, int? Volume);

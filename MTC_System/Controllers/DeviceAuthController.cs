using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MTC_System.Common;
using MTC_System.Data;
using MTC_System.DTOs.Devices;
using MTC_System.Models;
using MTC_System.Realtime;
using MTC_System.Services.Interfaces;

namespace MTC_System.Controllers;

[ApiController]
[Route("api/device-auth")]
public sealed class DeviceAuthController : ControllerBase
{
    private readonly IDeviceActivationService _activationService;
    private readonly MyDbContext _context;
    private readonly IDeviceCommandManager _commandManager;

    public DeviceAuthController(
        IDeviceActivationService activationService,
        MyDbContext context,
        IDeviceCommandManager commandManager)
    {
        _activationService = activationService;
        _context = context;
        _commandManager = commandManager;
    }

    [HttpPost("activate")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<DeviceTokenResponse>>> Activate(
        [FromBody] ActivateDeviceRequest request,
        CancellationToken cancellationToken)
    {
        var data = await _activationService.ActivateAsync(request, GetRemoteIpAddress(), cancellationToken);
        return Ok(ApiResponse<DeviceTokenResponse>.Ok(data, "Kích hoạt thiết bị thành công."));
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<DeviceTokenResponse>>> Refresh(
        [FromBody] RefreshDeviceTokenRequest request,
        CancellationToken cancellationToken)
    {
        var data = await _activationService.RefreshAsync(request, GetRemoteIpAddress(), cancellationToken);
        return Ok(ApiResponse<DeviceTokenResponse>.Ok(data, "Làm mới Device Token thành công."));
    }

    [HttpPost("heartbeat")]
    [Authorize(AuthenticationSchemes = "DeviceBearer")]
    public async Task<ActionResult<ApiResponse<object>>> Heartbeat(
        [FromBody] DeviceHeartbeatRequest request,
        CancellationToken cancellationToken)
    {
        var deviceId = GetDeviceId();
        if (deviceId == Guid.Empty)
            return Unauthorized(ApiResponse<object>.Fail("Token thiết bị không hợp lệ."));

        var device = await _context.Devices.FirstOrDefaultAsync(x => x.Id == deviceId && !x.IsDeleted, cancellationToken);
        if (device is null || !device.ActiveStatus)
            return Forbid();

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

        await _context.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<object>.Ok(new { deviceId, lastOnlineAt = device.LastOnlineAt }, "Heartbeat đã được ghi nhận."));
    }

    [HttpPost("ack")]
    [Authorize(AuthenticationSchemes = "DeviceBearer")]
    public async Task<ActionResult<ApiResponse<object>>> AcknowledgeCommand(
        [FromBody] DeviceCommandAckRequest request,
        CancellationToken cancellationToken)
    {
        var deviceId = GetDeviceId();
        if (deviceId == Guid.Empty)
            return Unauthorized(ApiResponse<object>.Fail("Token thiết bị không hợp lệ."));

        var ackResult = _commandManager.ProcessAck(request.CommandId, deviceId, request.Status, request.ErrorMessage);

        // Command not found -> return NotFound (Item 6)
        if (ackResult == CommandAckResult.NotFound)
            return NotFound(ApiResponse<object>.Fail("Lệnh không tồn tại hoặc đã hết hạn."));

        // Wrong device -> return Forbid
        if (ackResult == CommandAckResult.MismatchDevice)
            return Forbid();

        // Duplicate ACK -> return Idempotent Success
        if (ackResult == CommandAckResult.Duplicate)
            return Ok(ApiResponse<object>.Ok(new { commandId = request.CommandId, idempotent = true }, "ACK trùng lặp đã được ghi nhận thành công."));

        // Apply volume ONLY after successful ACK! (Item 8)
        var cmdState = _commandManager.GetCommand(request.CommandId);
        if (cmdState != null && cmdState.CommandType == "SetVolume" && request.Status.Equals("Success", StringComparison.OrdinalIgnoreCase) && cmdState.TargetVolume.HasValue)
        {
            var device = await _context.Devices.FirstOrDefaultAsync(x => x.Id == deviceId && !x.IsDeleted, cancellationToken);
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

        await _context.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<object>.Ok(new { commandId = request.CommandId, status = request.Status }, "Lệnh đã được ACK thành công."));
    }

    private Guid GetDeviceId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(claim, out var id) ? id : Guid.Empty;
    }

    private string? GetRemoteIpAddress() => HttpContext.Connection.RemoteIpAddress?.ToString();
}

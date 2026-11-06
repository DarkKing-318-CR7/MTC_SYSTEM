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

    /// <summary>
    /// Báo cáo kết quả phát sóng bản tin qua REST API (tương đương SignalR Hub ReportPlaybackResult)
    /// </summary>
    [HttpPost("playback-result")]
    [Authorize(AuthenticationSchemes = "DeviceBearer")]
    public async Task<IActionResult> ReportPlaybackResult([FromBody] PlaybackReportRequest request, CancellationToken cancellationToken)
    {
        var deviceId = GetDeviceId();
        if (deviceId == Guid.Empty)
            return Unauthorized(ApiResponse<bool>.Fail("Token thiết bị không hợp lệ."));

        var device = await _context.Devices.FirstOrDefaultAsync(x => x.Id == deviceId && !x.IsDeleted, cancellationToken);
        if (device == null || !device.ActiveStatus)
            return NotFound(ApiResponse<bool>.Fail("Thiết bị không tồn tại hoặc chưa kích hoạt."));

        var broadcast = await _context.Broadcasts
            .Include(b => b.Schedules)
                .ThenInclude(s => s.BroadcastDevices)
            .FirstOrDefaultAsync(b => b.Id == request.BroadcastId && !b.IsDeleted, cancellationToken);

        if (broadcast == null)
            return NotFound(ApiResponse<bool>.Fail("Bản tin không tồn tại."));

        // Kiểm tra phân quyền phạm vi cơ sở / thiết bị
        bool isSameFacility = broadcast.FacilityId == device.FacilityId && broadcast.OrganizationId == device.OrganizationId;
        bool isTargetedDevice = broadcast.Schedules.Any(s => !s.IsDeleted && s.BroadcastDevices.Any(bd => !bd.IsDeleted && bd.DeviceId == deviceId));
        if (!isSameFacility && !isTargetedDevice)
        {
            return Forbid();
        }

        // Hoàn tất lịch sử "Đang phát" hiện có thay vì tạo bản ghi trùng lặp
        var existingHistory = await _context.DevicePlayHistories
            .Where(h => h.DeviceId == deviceId && h.BroadcastId == request.BroadcastId && h.PlayResult == "Đang phát")
            .OrderByDescending(h => h.StartedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(request.Result))
        {
            return BadRequest(ApiResponse<bool>.Fail("Kết quả phát sóng không được để trống."));
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
            return BadRequest(ApiResponse<bool>.Fail($"Giá trị kết quả phát '{request.Result}' không hợp lệ. Chỉ chấp nhận 'Hoàn thành' hoặc 'Thất bại'."));
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
                               (h.PlayResult == "Hoàn thành" || h.PlayResult == "Thành công" || h.PlayResult == "Thất bại"), cancellationToken);

            if (alreadyCompleted)
            {
                return Ok(ApiResponse<bool>.Ok(true, "Kết quả phát sóng đã được ghi nhận trước đó (idempotent)."));
            }

            // Không tìm thấy phiên phát sóng nào được dispatch tới thiết bị này -> Từ chối báo cáo giả
            return NotFound(ApiResponse<bool>.Fail("Không tìm thấy phiên phát sóng đang thực hiện của bản tin này trên thiết bị."));
        }

        device.PlayStatus = "Dừng phát bản tin";
        device.UpdatedAt = DateTimeOffset.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<bool>.Ok(true, "Báo cáo kết quả phát sóng thành công."));
    }

    private Guid GetDeviceId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(claim, out var id) ? id : Guid.Empty;
    }

    private string? GetRemoteIpAddress() => HttpContext.Connection.RemoteIpAddress?.ToString();
}

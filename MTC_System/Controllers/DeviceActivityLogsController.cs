using Microsoft.AspNetCore.Mvc;
using MTC_System.Common;
using MTC_System.DTOs.DeviceActivityLogs;
using MTC_System.Services.Interfaces;

namespace MTC_System.Controllers;

[ApiController]
[Route("api/device-activity-logs")]
public sealed class DeviceActivityLogsController : ControllerBase
{
    private readonly IDeviceActivityLogService _service;

    public DeviceActivityLogsController(IDeviceActivityLogService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<DeviceActivityLogResponse>>>> GetAll(
        [FromQuery] Guid? deviceId,
        [FromQuery] string? actionType,
        [FromQuery] string? result,
        CancellationToken cancellationToken)
    {
        var data = await _service.GetAllAsync(deviceId, actionType, result, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<DeviceActivityLogResponse>>.Ok(data));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<DeviceActivityLogResponse>>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var data = await _service.GetByIdAsync(id, cancellationToken);
        return Ok(ApiResponse<DeviceActivityLogResponse>.Ok(data));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<DeviceActivityLogResponse>>> Create(
        [FromBody] CreateDeviceActivityLogRequest request,
        CancellationToken cancellationToken)
    {
        var data = await _service.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = data.Id }, ApiResponse<DeviceActivityLogResponse>.Ok(data, "Tạo nhật ký thiết bị thành công."));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<DeviceActivityLogResponse>>> Update(
        Guid id,
        [FromBody] UpdateDeviceActivityLogRequest request,
        CancellationToken cancellationToken)
    {
        var data = await _service.UpdateAsync(id, request, cancellationToken);
        return Ok(ApiResponse<DeviceActivityLogResponse>.Ok(data, "Cập nhật nhật ký thiết bị thành công."));
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _service.DeleteAsync(id, cancellationToken);
        return Ok(ApiResponse<object>.Ok(new { id }, "Xóa nhật ký thiết bị thành công."));
    }
}

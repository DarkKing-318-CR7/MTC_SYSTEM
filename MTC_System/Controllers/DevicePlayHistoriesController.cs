using Microsoft.AspNetCore.Mvc;
using MTC_System.Common;
using MTC_System.DTOs.DevicePlayHistories;
using MTC_System.Services.Interfaces;

namespace MTC_System.Controllers;

[ApiController]
[Route("api/device-play-histories")]
public sealed class DevicePlayHistoriesController : ControllerBase
{
    private readonly IDevicePlayHistoryService _service;

    public DevicePlayHistoriesController(IDevicePlayHistoryService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<DevicePlayHistoryResponse>>>> GetAll(
        [FromQuery] Guid? deviceId,
        [FromQuery] Guid? broadcastId,
        [FromQuery] string? result,
        CancellationToken cancellationToken)
    {
        var data = await _service.GetAllAsync(deviceId, broadcastId, result, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<DevicePlayHistoryResponse>>.Ok(data));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<DevicePlayHistoryResponse>>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var data = await _service.GetByIdAsync(id, cancellationToken);
        return Ok(ApiResponse<DevicePlayHistoryResponse>.Ok(data));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<DevicePlayHistoryResponse>>> Create(
        [FromBody] CreateDevicePlayHistoryRequest request,
        CancellationToken cancellationToken)
    {
        var data = await _service.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = data.Id }, ApiResponse<DevicePlayHistoryResponse>.Ok(data, "Tạo lịch sử phát thiết bị thành công."));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<DevicePlayHistoryResponse>>> Update(
        Guid id,
        [FromBody] UpdateDevicePlayHistoryRequest request,
        CancellationToken cancellationToken)
    {
        var data = await _service.UpdateAsync(id, request, cancellationToken);
        return Ok(ApiResponse<DevicePlayHistoryResponse>.Ok(data, "Cập nhật lịch sử phát thiết bị thành công."));
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _service.DeleteAsync(id, cancellationToken);
        return Ok(ApiResponse<object>.Ok(new { id }, "Xóa lịch sử phát thiết bị thành công."));
    }
}

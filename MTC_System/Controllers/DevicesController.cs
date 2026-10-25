using Microsoft.AspNetCore.Mvc;
using MTC_System.Common;
using MTC_System.DTOs.Devices;
using MTC_System.Services.Interfaces;

namespace MTC_System.Controllers;

[ApiController]
[Route("api/devices")]
public sealed class DevicesController : ControllerBase
{
    private readonly IDeviceService _service;

    public DevicesController(IDeviceService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<DeviceResponse>>>> GetAll(
        [FromQuery] Guid? organizationId,
        [FromQuery] Guid? facilityId,
        [FromQuery] string? search,
        CancellationToken cancellationToken)
    {
        var data = await _service.GetAllAsync(organizationId, facilityId, search, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<DeviceResponse>>.Ok(data));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<DeviceResponse>>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var data = await _service.GetByIdAsync(id, cancellationToken);
        return Ok(ApiResponse<DeviceResponse>.Ok(data));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<DeviceResponse>>> Create(
        [FromBody] CreateDeviceRequest request,
        CancellationToken cancellationToken)
    {
        var data = await _service.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = data.Id }, ApiResponse<DeviceResponse>.Ok(data, "Tạo thiết bị thành công."));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<DeviceResponse>>> Update(
        Guid id,
        [FromBody] UpdateDeviceRequest request,
        CancellationToken cancellationToken)
    {
        var data = await _service.UpdateAsync(id, request, cancellationToken);
        return Ok(ApiResponse<DeviceResponse>.Ok(data, "Cập nhật thiết bị thành công."));
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _service.DeleteAsync(id, cancellationToken);
        return Ok(ApiResponse<object>.Ok(new { id }, "Xóa thiết bị thành công."));
    }
}

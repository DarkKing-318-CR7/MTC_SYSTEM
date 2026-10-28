using Microsoft.AspNetCore.Mvc;
using MTC_System.Common;
using MTC_System.DTOs.DeviceVendors;
using MTC_System.Services.Interfaces;

namespace MTC_System.Controllers;

[ApiController]
[Route("api/device-vendors")]
public sealed class DeviceVendorsController : ControllerBase
{
    private readonly IDeviceVendorService _service;

    public DeviceVendorsController(IDeviceVendorService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<DeviceVendorResponse>>>> GetAll(
        [FromQuery] Guid? organizationId,
        [FromQuery] string? search,
        CancellationToken cancellationToken)
    {
        var data = await _service.GetAllAsync(organizationId, search, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<DeviceVendorResponse>>.Ok(data));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<DeviceVendorResponse>>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var data = await _service.GetByIdAsync(id, cancellationToken);
        return Ok(ApiResponse<DeviceVendorResponse>.Ok(data));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<DeviceVendorResponse>>> Create(
        [FromBody] CreateDeviceVendorRequest request,
        CancellationToken cancellationToken)
    {
        var data = await _service.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = data.Id }, ApiResponse<DeviceVendorResponse>.Ok(data, "Tạo nhà phân phối thành công."));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<DeviceVendorResponse>>> Update(
        Guid id,
        [FromBody] UpdateDeviceVendorRequest request,
        CancellationToken cancellationToken)
    {
        var data = await _service.UpdateAsync(id, request, cancellationToken);
        return Ok(ApiResponse<DeviceVendorResponse>.Ok(data, "Cập nhật nhà phân phối thành công."));
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _service.DeleteAsync(id, cancellationToken);
        return Ok(ApiResponse<object>.Ok(new { id }, "Xóa nhà phân phối thành công."));
    }
}

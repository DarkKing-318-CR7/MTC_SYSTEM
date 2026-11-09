using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MTC_System.Common;
using MTC_System.DTOs.DeviceVendors;
using MTC_System.Services.Interfaces;

namespace MTC_System.Controllers;

[ApiController]
[Route("api/device-vendors")]
[Authorize(Policy = "DeviceView")]
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
        var scope = User.GetUserScope();
        var data = await _service.GetAllAsync(scope, organizationId, search, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<DeviceVendorResponse>>.Ok(data));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<DeviceVendorResponse>>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var scope = User.GetUserScope();
        var data = await _service.GetByIdAsync(id, scope, cancellationToken);
        return Ok(ApiResponse<DeviceVendorResponse>.Ok(data));
    }

    [HttpPost]
    [Authorize(Policy = "DeviceManage")]
    public async Task<ActionResult<ApiResponse<DeviceVendorResponse>>> Create(
        [FromBody] CreateDeviceVendorRequest request,
        CancellationToken cancellationToken)
    {
        var scope = User.GetUserScope();
        var data = await _service.CreateAsync(request, scope, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = data.Id }, ApiResponse<DeviceVendorResponse>.Ok(data, "Tạo nhà phân phối thành công."));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "DeviceManage")]
    public async Task<ActionResult<ApiResponse<DeviceVendorResponse>>> Update(
        Guid id,
        [FromBody] UpdateDeviceVendorRequest request,
        CancellationToken cancellationToken)
    {
        var scope = User.GetUserScope();
        var data = await _service.UpdateAsync(id, request, scope, cancellationToken);
        return Ok(ApiResponse<DeviceVendorResponse>.Ok(data, "Cập nhật nhà phân phối thành công."));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "DeviceManage")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(Guid id, CancellationToken cancellationToken)
    {
        var scope = User.GetUserScope();
        await _service.DeleteAsync(id, scope, cancellationToken);
        return Ok(ApiResponse<object>.Ok(new { id }, "Xóa nhà phân phối thành công."));
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MTC_System.Common;
using MTC_System.DTOs.DeviceErrorCodes;
using MTC_System.Services.Interfaces;

namespace MTC_System.Controllers;

[ApiController]
[Route("api/device-error-codes")]
[Authorize(Policy = "DeviceView")]
public sealed class DeviceErrorCodesController : ControllerBase
{
    private readonly IDeviceErrorCodeService _service;

    public DeviceErrorCodesController(IDeviceErrorCodeService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<DeviceErrorCodeResponse>>>> GetAll(
        [FromQuery] string? search,
        [FromQuery] string? severity,
        CancellationToken cancellationToken)
    {
        var data = await _service.GetAllAsync(search, severity, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<DeviceErrorCodeResponse>>.Ok(data));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<DeviceErrorCodeResponse>>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var data = await _service.GetByIdAsync(id, cancellationToken);
        return Ok(ApiResponse<DeviceErrorCodeResponse>.Ok(data));
    }

    [HttpPost]
    [Authorize(Policy = "DeviceAdminOnly")]
    public async Task<ActionResult<ApiResponse<DeviceErrorCodeResponse>>> Create(
        [FromBody] CreateDeviceErrorCodeRequest request,
        CancellationToken cancellationToken)
    {
        var scope = User.GetUserScope();
        var data = await _service.CreateAsync(request, scope, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = data.Id }, ApiResponse<DeviceErrorCodeResponse>.Ok(data, "Tạo mã lỗi thiết bị thành công."));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "DeviceAdminOnly")]
    public async Task<ActionResult<ApiResponse<DeviceErrorCodeResponse>>> Update(
        Guid id,
        [FromBody] UpdateDeviceErrorCodeRequest request,
        CancellationToken cancellationToken)
    {
        var scope = User.GetUserScope();
        var data = await _service.UpdateAsync(id, request, scope, cancellationToken);
        return Ok(ApiResponse<DeviceErrorCodeResponse>.Ok(data, "Cập nhật mã lỗi thiết bị thành công."));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "DeviceAdminOnly")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(Guid id, CancellationToken cancellationToken)
    {
        var scope = User.GetUserScope();
        await _service.DeleteAsync(id, scope, cancellationToken);
        return Ok(ApiResponse<object>.Ok(new { id }, "Xóa mã lỗi thiết bị thành công."));
    }
}

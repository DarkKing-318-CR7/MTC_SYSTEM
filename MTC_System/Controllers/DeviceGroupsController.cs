using Microsoft.AspNetCore.Mvc;
using MTC_System.Common;
using MTC_System.DTOs.DeviceGroups;
using MTC_System.Services.Interfaces;

namespace MTC_System.Controllers;

[ApiController]
[Route("api/device-groups")]
public sealed class DeviceGroupsController : ControllerBase
{
    private readonly IDeviceGroupService _service;

    public DeviceGroupsController(IDeviceGroupService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<DeviceGroupResponse>>>> GetAll(
        [FromQuery] Guid? organizationId,
        [FromQuery] Guid? facilityId,
        [FromQuery] string? search,
        CancellationToken cancellationToken)
    {
        var data = await _service.GetAllAsync(organizationId, facilityId, search, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<DeviceGroupResponse>>.Ok(data));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<DeviceGroupResponse>>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var data = await _service.GetByIdAsync(id, cancellationToken);
        return Ok(ApiResponse<DeviceGroupResponse>.Ok(data));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<DeviceGroupResponse>>> Create(
        [FromBody] CreateDeviceGroupRequest request,
        CancellationToken cancellationToken)
    {
        var data = await _service.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = data.Id }, ApiResponse<DeviceGroupResponse>.Ok(data, "Tạo nhóm thiết bị thành công."));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<DeviceGroupResponse>>> Update(
        Guid id,
        [FromBody] UpdateDeviceGroupRequest request,
        CancellationToken cancellationToken)
    {
        var data = await _service.UpdateAsync(id, request, cancellationToken);
        return Ok(ApiResponse<DeviceGroupResponse>.Ok(data, "Cập nhật nhóm thiết bị thành công."));
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _service.DeleteAsync(id, cancellationToken);
        return Ok(ApiResponse<object>.Ok(new { id }, "Xóa nhóm thiết bị thành công."));
    }
}

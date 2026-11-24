using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MTC_System.Common;
using MTC_System.DTOs.DeviceGroups;
using MTC_System.Services.Interfaces;

namespace MTC_System.Controllers;

[ApiController]
[Route("api/device-groups")]
[Authorize(Policy = "DeviceView")]
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
        var scope = User.GetUserScope();
        var data = await _service.GetAllAsync(scope, organizationId, facilityId, search, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<DeviceGroupResponse>>.Ok(data));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<DeviceGroupResponse>>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var scope = User.GetUserScope();
        var data = await _service.GetByIdAsync(id, scope, cancellationToken);
        return Ok(ApiResponse<DeviceGroupResponse>.Ok(data));
    }

    [HttpPost]
    [Authorize(Policy = "DeviceManage")]
    public async Task<ActionResult<ApiResponse<DeviceGroupResponse>>> Create(
        [FromBody] CreateDeviceGroupRequest request,
        CancellationToken cancellationToken)
    {
        var scope = User.GetUserScope();
        var data = await _service.CreateAsync(request, scope, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = data.Id }, ApiResponse<DeviceGroupResponse>.Ok(data, "Tạo nhóm thiết bị thành công."));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "DeviceManage")]
    public async Task<ActionResult<ApiResponse<DeviceGroupResponse>>> Update(
        Guid id,
        [FromBody] UpdateDeviceGroupRequest request,
        CancellationToken cancellationToken)
    {
        var scope = User.GetUserScope();
        var data = await _service.UpdateAsync(id, request, scope, cancellationToken);
        return Ok(ApiResponse<DeviceGroupResponse>.Ok(data, "Cập nhật nhóm thiết bị thành công."));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "DeviceManage")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(Guid id, CancellationToken cancellationToken)
    {
        var scope = User.GetUserScope();
        await _service.DeleteAsync(id, scope, cancellationToken);
        return Ok(ApiResponse<object>.Ok(new { id }, "Xóa nhóm thiết bị thành công."));
    }
}

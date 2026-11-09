using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MTC_System.Common;
using MTC_System.DTOs.Devices;
using MTC_System.Services.Interfaces;

namespace MTC_System.Controllers;

[ApiController]
[Route("api/devices")]
[Authorize(Policy = "DeviceView")]
public sealed class DevicesController : ControllerBase
{
    private readonly IDeviceService _service;
    private readonly IDeviceActivationService _activationService;

    public DevicesController(IDeviceService service, IDeviceActivationService activationService)
    {
        _service = service;
        _activationService = activationService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<DeviceResponse>>>> GetAll(
        [FromQuery] Guid? organizationId,
        [FromQuery] Guid? facilityId,
        [FromQuery] string? search,
        CancellationToken cancellationToken)
    {
        var scope = User.GetUserScope();
        var data = await _service.GetAllAsync(scope, organizationId, facilityId, search, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<DeviceResponse>>.Ok(data));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<DeviceResponse>>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var scope = User.GetUserScope();
        var data = await _service.GetByIdAsync(id, scope, cancellationToken);
        return Ok(ApiResponse<DeviceResponse>.Ok(data));
    }

    [HttpGet("map")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<DeviceMapItemResponse>>>> GetMap(
        [FromQuery] Guid? organizationId,
        [FromQuery] Guid? facilityId,
        CancellationToken cancellationToken)
    {
        var scope = User.GetUserScope();
        var data = await _service.GetMapAsync(scope, organizationId, facilityId, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<DeviceMapItemResponse>>.Ok(data));
    }

    [HttpPost]
    [Authorize(Policy = "DeviceManage")]
    public async Task<ActionResult<ApiResponse<DeviceResponse>>> Create(
        [FromBody] CreateDeviceRequest request,
        CancellationToken cancellationToken)
    {
        var scope = User.GetUserScope();
        var data = await _service.CreateAsync(request, scope, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = data.Id }, ApiResponse<DeviceResponse>.Ok(data, "Tạo thiết bị thành công."));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "DeviceManage")]
    public async Task<ActionResult<ApiResponse<DeviceResponse>>> Update(
        Guid id,
        [FromBody] UpdateDeviceRequest request,
        CancellationToken cancellationToken)
    {
        var scope = User.GetUserScope();
        var data = await _service.UpdateAsync(id, request, scope, cancellationToken);
        return Ok(ApiResponse<DeviceResponse>.Ok(data, "Cập nhật thiết bị thành công."));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "DeviceManage")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(Guid id, CancellationToken cancellationToken)
    {
        var scope = User.GetUserScope();
        await _service.DeleteAsync(id, scope, cancellationToken);
        return Ok(ApiResponse<object>.Ok(new { id }, "Xóa thiết bị thành công."));
    }

    [HttpPost("{id:guid}/activation-code")]
    [Authorize(Policy = "DeviceManage")]
    public async Task<ActionResult<ApiResponse<DeviceActivationCodeResponse>>> GenerateActivationCode(
        Guid id,
        CancellationToken cancellationToken)
    {
        var admin = new DeviceActivationAdminContext(
            ParseClaim(ClaimTypes.NameIdentifier),
            ParseClaim("organizationId"),
            ParseClaim("facilityId"),
            User.IsInRole("ADMINISTRATOR") || User.IsInRole("Administrator"));
        var data = await _activationService.GenerateActivationCodeAsync(id, admin, cancellationToken);
        return Ok(ApiResponse<DeviceActivationCodeResponse>.Ok(data, "Sinh mã kích hoạt thiết bị thành công."));
    }

    private Guid? ParseClaim(string claimType) =>
        Guid.TryParse(User.FindFirstValue(claimType), out var value) ? value : null;
}

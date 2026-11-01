using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MTC_System.Common;
using MTC_System.DTOs.Devices;
using MTC_System.Services.Interfaces;

namespace MTC_System.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/device-auth")]
public sealed class DeviceAuthController : ControllerBase
{
    private readonly IDeviceActivationService _activationService;

    public DeviceAuthController(IDeviceActivationService activationService) =>
        _activationService = activationService;

    [HttpPost("activate")]
    public async Task<ActionResult<ApiResponse<DeviceTokenResponse>>> Activate(
        [FromBody] ActivateDeviceRequest request,
        CancellationToken cancellationToken)
    {
        var data = await _activationService.ActivateAsync(request, GetRemoteIpAddress(), cancellationToken);
        return Ok(ApiResponse<DeviceTokenResponse>.Ok(data, "Kích hoạt thiết bị thành công."));
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<ApiResponse<DeviceTokenResponse>>> Refresh(
        [FromBody] RefreshDeviceTokenRequest request,
        CancellationToken cancellationToken)
    {
        var data = await _activationService.RefreshAsync(request, GetRemoteIpAddress(), cancellationToken);
        return Ok(ApiResponse<DeviceTokenResponse>.Ok(data, "Làm mới Device Token thành công."));
    }

    private string? GetRemoteIpAddress() => HttpContext.Connection.RemoteIpAddress?.ToString();
}

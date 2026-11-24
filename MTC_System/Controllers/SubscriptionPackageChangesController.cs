using Microsoft.AspNetCore.Mvc;
using MTC_System.Common;
using MTC_System.DTOs.Packages;
using MTC_System.Services;

namespace MTC_System.Controllers;

[ApiController]
[Route("api/Subscriptions/{subscriptionId:guid}/package-changes")]
public sealed class SubscriptionPackageChangesController(PackageChangeService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<SubscriptionPackageChangeResponseDto>>>> Get(Guid subscriptionId)
    {
        var result = await service.GetForSubscriptionAsync(subscriptionId);
        return result.Success ? Ok(result) : NotFound(result);
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<SubscriptionPackageChangeResponseDto>>> Schedule(Guid subscriptionId, PackageChangeRequestDto dto)
    {
        var result = await service.ScheduleAsync(subscriptionId, dto);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpDelete("{changeId:guid}")]
    public async Task<ActionResult<ApiResponse<bool>>> Cancel(Guid subscriptionId, Guid changeId)
    {
        var result = await service.CancelAsync(subscriptionId, changeId);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}

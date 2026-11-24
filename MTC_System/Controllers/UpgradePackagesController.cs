using Microsoft.AspNetCore.Mvc;
using MTC_System.Common;
using MTC_System.DTOs.Packages;
using MTC_System.Services;

namespace MTC_System.Controllers;

[ApiController]
[Route("api/UpgradePackages")]
public sealed class UpgradePackagesController(UpgradePackageService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<UpgradePackageResponseDto>>>> GetAll() => Ok(await service.GetAllAsync());

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<UpgradePackageResponseDto>>> GetById(Guid id)
    {
        var result = await service.GetByIdAsync(id);
        return result.Success ? Ok(result) : NotFound(result);
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<UpgradePackageResponseDto>>> Create(UpgradePackageUpsertDto dto)
    {
        var result = await service.CreateAsync(dto);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<UpgradePackageResponseDto>>> Update(Guid id, UpgradePackageUpsertDto dto)
    {
        var result = await service.UpdateAsync(id, dto);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(Guid id)
    {
        var result = await service.DeleteAsync(id);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpGet("{id:guid}/purchasers")]
    public async Task<ActionResult<ApiResponse<List<UpgradePurchaserDto>>>> Purchasers(Guid id)
    {
        var result = await service.GetPurchasersAsync(id);
        return result.Success ? Ok(result) : NotFound(result);
    }
}

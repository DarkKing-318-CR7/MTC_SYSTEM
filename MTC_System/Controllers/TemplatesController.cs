using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MTC_System.Common;
using MTC_System.DTOs.Templates;
using MTC_System.Services.Interfaces;

namespace MTC_System.Controllers;

[ApiController]
[Authorize]
[Route("api/templates")]
public sealed class TemplatesController : ControllerBase
{
    private readonly ITemplateService _service;

    public TemplatesController(ITemplateService service) => _service = service;

    [HttpGet]
    [Authorize(Roles = "ADMINISTRATOR,CREATOR")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<TemplateResponse>>>> GetAll(
        [FromQuery] Guid? organizationId,
        [FromQuery] Guid? facilityId,
        CancellationToken cancellationToken)
    {
        var data = await _service.GetAllAsync(User.GetUserScope(), organizationId, facilityId, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<TemplateResponse>>.Ok(data));
    }

    [HttpGet("{id:guid}")]
    [Authorize(Roles = "ADMINISTRATOR,CREATOR")]
    public async Task<ActionResult<ApiResponse<TemplateResponse>>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var data = await _service.GetByIdAsync(id, User.GetUserScope(), cancellationToken);
        return Ok(ApiResponse<TemplateResponse>.Ok(data));
    }

    [HttpPost]
    [Authorize(Roles = "ADMINISTRATOR,CREATOR")]
    public async Task<ActionResult<ApiResponse<TemplateResponse>>> Create(
        [FromBody] TemplateUpsertRequest request,
        CancellationToken cancellationToken)
    {
        var data = await _service.CreateAsync(request, User.GetUserScope(), cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = data.Id }, ApiResponse<TemplateResponse>.Ok(data, "Tạo Template thành công."));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "ADMINISTRATOR,CREATOR")]
    public async Task<ActionResult<ApiResponse<TemplateResponse>>> Update(
        Guid id,
        [FromBody] TemplateUpsertRequest request,
        CancellationToken cancellationToken)
    {
        var data = await _service.UpdateAsync(id, request, User.GetUserScope(), cancellationToken);
        return Ok(ApiResponse<TemplateResponse>.Ok(data, "Cập nhật Template thành công."));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "ADMINISTRATOR,CREATOR")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _service.DeleteAsync(id, User.GetUserScope(), cancellationToken);
        return Ok(ApiResponse<object>.Ok(new { id }, "Xóa Template thành công."));
    }
}

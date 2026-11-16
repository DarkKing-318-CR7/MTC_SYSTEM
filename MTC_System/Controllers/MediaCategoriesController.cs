using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MTC_System.Common;
using MTC_System.DTOs.MediaCategories;
using MTC_System.Services;
using MTC_System.Services.Interfaces;

namespace MTC_System.Controllers;

[ApiController]
[Authorize]
[Route("api/media-categories")]
public sealed class MediaCategoriesController : ControllerBase
{
    private readonly IMediaCategoryService _service;

    public MediaCategoriesController(IMediaCategoryService service) => _service = service;

    [HttpGet]
    [Authorize(Roles = "ADMINISTRATOR,CREATOR,APPROVER")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<MediaCategoryResponse>>>> GetAll(CancellationToken cancellationToken)
    {
        if (!TryGetScope(out var organizationId)) return Forbid();
        var data = await _service.GetAllAsync(organizationId, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<MediaCategoryResponse>>.Ok(data));
    }

    [HttpGet("{id:guid}")]
    [Authorize(Roles = "ADMINISTRATOR,CREATOR,APPROVER")]
    public async Task<ActionResult<ApiResponse<MediaCategoryResponse>>> GetById(Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetScope(out var organizationId)) return Forbid();
        var data = await _service.GetByIdAsync(id, organizationId, cancellationToken);
        return Ok(ApiResponse<MediaCategoryResponse>.Ok(data));
    }

    [HttpPost]
    [Authorize(Roles = "ADMINISTRATOR,CREATOR")]
    public async Task<ActionResult<ApiResponse<MediaCategoryResponse>>> Create([FromBody] CreateMediaCategoryRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetScope(out var organizationId) || !TryGetUserId(out var userId)) return Forbid();
        var data = await _service.CreateAsync(request, organizationId, userId, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = data.Id }, ApiResponse<MediaCategoryResponse>.Ok(data, "Tạo lĩnh vực thành công."));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "ADMINISTRATOR,CREATOR")]
    public async Task<ActionResult<ApiResponse<MediaCategoryResponse>>> Update(Guid id, [FromBody] UpdateMediaCategoryRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetScope(out var organizationId) || !TryGetUserId(out var userId)) return Forbid();
        var data = await _service.UpdateAsync(id, request, organizationId, userId, cancellationToken);
        return Ok(ApiResponse<MediaCategoryResponse>.Ok(data, "Cập nhật lĩnh vực thành công."));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "ADMINISTRATOR,CREATOR")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetScope(out var organizationId) || !TryGetUserId(out var userId)) return Forbid();
        await _service.DeleteAsync(id, organizationId, userId, cancellationToken);
        return Ok(ApiResponse<object>.Ok(new { id }, "Xóa lĩnh vực thành công."));
    }

    private bool TryGetScope(out Guid organizationId)
    {
        organizationId = Guid.Empty;
        if (!Guid.TryParse(User.FindFirstValue("organizationId"), out organizationId)) return false;
        return !string.IsNullOrEmpty(User.FindFirstValue("facilityId")) || User.IsInRole("ADMINISTRATOR");
    }
    private bool TryGetUserId(out Guid userId) => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId);
}

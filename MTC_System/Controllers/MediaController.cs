using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MTC_System.Common;
using MTC_System.DTOs.Media;
using MTC_System.Services;
using MTC_System.Services.Interfaces;

namespace MTC_System.Controllers;

[ApiController]
[Authorize]
[Route("api/media")]
public sealed class MediaController : ControllerBase
{
    private readonly IMediaService _service;

    public MediaController(IMediaService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<MediaResponse>>>> GetAll(CancellationToken cancellationToken)
    {
        if (!TryGetScope(out var organizationId, out var facilityId)) return Forbid();
        var data = await _service.GetAllAsync(organizationId, facilityId, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<MediaResponse>>.Ok(data));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<MediaResponse>>> GetById(Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetScope(out var organizationId, out var facilityId)) return Forbid();
        var data = await _service.GetByIdAsync(id, organizationId, facilityId, cancellationToken);
        return Ok(ApiResponse<MediaResponse>.Ok(data));
    }

    [HttpPost]
    [Authorize(Roles = "CREATOR,ADMINISTRATOR,Administrator")]
    public async Task<ActionResult<ApiResponse<MediaResponse>>> Create([FromBody] CreateMediaRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetScope(out var organizationId, out var facilityId) || !TryGetUserId(out var userId)) return Forbid();
        if (!await _service.CanAccessFacilityAsync(organizationId, facilityId, request.FacilityId, cancellationToken)) return Forbid();
        var targetFacilityId = request.FacilityId;
        var data = await _service.CreateAsync(request, organizationId, targetFacilityId, userId, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = data.Id }, ApiResponse<MediaResponse>.Ok(data, "Tạo media thành công; nguồn URL chỉ được lưu, không tải về máy chủ."));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "CREATOR,ADMINISTRATOR,Administrator")]
    public async Task<ActionResult<ApiResponse<MediaResponse>>> Update(Guid id, [FromBody] UpdateMediaRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetScope(out var organizationId, out var facilityId) || !TryGetUserId(out var userId)) return Forbid();
        var data = await _service.UpdateAsync(id, request, organizationId, facilityId, userId, cancellationToken);
        return Ok(ApiResponse<MediaResponse>.Ok(data, "Cập nhật media thành công."));
    }

    [HttpPost("{id:guid}/approve")]
    [Authorize(Roles = "APPROVER,ADMINISTRATOR,Administrator")]
    public async Task<ActionResult<ApiResponse<MediaResponse>>> Approve(Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetScope(out var organizationId, out var facilityId) || !TryGetUserId(out var userId)) return Forbid();
        var data = await _service.ApproveAsync(id, organizationId, facilityId, userId, cancellationToken);
        return Ok(ApiResponse<MediaResponse>.Ok(data, "Duyệt media thành công."));
    }

    [HttpPost("{id:guid}/reject")]
    [Authorize(Roles = "APPROVER,ADMINISTRATOR,Administrator")]
    public async Task<ActionResult<ApiResponse<MediaResponse>>> Reject(Guid id, [FromBody] ReviewMediaRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetScope(out var organizationId, out var facilityId) || !TryGetUserId(out var userId)) return Forbid();
        var data = await _service.RejectAsync(id, request.RejectionReason, organizationId, facilityId, userId, cancellationToken);
        return Ok(ApiResponse<MediaResponse>.Ok(data, "Từ chối duyệt media thành công."));
    }

    private bool TryGetScope(out Guid organizationId, out Guid? facilityId)
    {
        organizationId = Guid.Empty;
        facilityId = null;
        var organization = User.FindFirstValue("organizationId");
        if (!Guid.TryParse(organization, out organizationId)) return false;
        var facility = User.FindFirstValue("facilityId");
        if (string.IsNullOrEmpty(facility)) return true;
        if (!Guid.TryParse(facility, out var parsed)) return false;
        facilityId = parsed;
        return true;
    }

    private bool TryGetUserId(out Guid userId) => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId);
}

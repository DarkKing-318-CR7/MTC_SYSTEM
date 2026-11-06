using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MTC_System.Common;
using MTC_System.Data;
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
    private readonly MyDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _environment;

    public MediaController(
        IMediaService service,
        MyDbContext context,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        _service = service;
        _context = context;
        _configuration = configuration;
        _environment = environment;
    }

    /// <summary>
    /// Phát / tải trực tiếp luồng nội dung tệp Media (hỗ trợ HTTP Range requests cho trình phát media của thiết bị)
    /// </summary>
    [HttpGet("{id:guid}/stream")]
    [AllowAnonymous]
    public async Task<IActionResult> StreamMedia(Guid id, CancellationToken cancellationToken)
    {
        var media = await _context.Media.FirstOrDefaultAsync(m => m.Id == id && !m.IsDeleted, cancellationToken);
        if (media == null)
            return NotFound(ApiResponse<string>.Fail("Không tìm thấy tệp media."));

        if (!string.IsNullOrWhiteSpace(media.Url))
        {
            return Redirect(media.Url);
        }

        if (string.IsNullOrWhiteSpace(media.FilePath))
        {
            return NotFound(ApiResponse<string>.Fail("Media không có đường dẫn tệp."));
        }

        var configuredRoot = _configuration["MediaUpload:StorageRoot"] ?? "storage";
        var storageRoot = Path.GetFullPath(Path.Combine(_environment.ContentRootPath, configuredRoot));
        var fullPath = Path.Combine(storageRoot, media.FilePath.TrimStart('/', '\\'));

        if (!System.IO.File.Exists(fullPath))
        {
            fullPath = Path.Combine(_environment.ContentRootPath, media.FilePath.TrimStart('/', '\\'));
        }

        if (!System.IO.File.Exists(fullPath))
        {
            return NotFound(ApiResponse<string>.Fail("Tệp tin media không tồn tại trên máy chủ lưu trữ."));
        }

        var extension = Path.GetExtension(media.FileName ?? media.FilePath).ToLowerInvariant();
        var contentType = extension switch
        {
            ".mp3" => "audio/mpeg",
            ".wav" => "audio/wav",
            ".ogg" => "audio/ogg",
            ".mp4" => "video/mp4",
            ".webm" => "video/webm",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            _ => "application/octet-stream"
        };

        return PhysicalFile(fullPath, contentType, enableRangeProcessing: true);
    }

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

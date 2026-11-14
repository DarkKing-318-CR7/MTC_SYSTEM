using System.Security.Claims;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MTC_System.Common;
using MTC_System.Data;
using MTC_System.DTOs.Media;
using MTC_System.Models;
using MTC_System.Services;
using MTC_System.Services.Implements;
using MTC_System.Services.Interfaces;

namespace MTC_System.Controllers;

[ApiController]
[Authorize]
[Route("api/media")]
public sealed class MediaController : ControllerBase
{
    private static readonly Regex HlsSegmentName = new("^segment_[0-9]{5,8}\\.ts$", RegexOptions.CultureInvariant | RegexOptions.Compiled);
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

    [HttpGet("{id:guid}/hls/index.m3u8")]
    [Authorize(Roles = "ADMINISTRATOR,CREATOR,APPROVER")]
    public async Task<IActionResult> GetHlsPlaylist(Guid id, CancellationToken cancellationToken)
    {
        var media = await GetPlayableHlsMediaAsync(id, cancellationToken);
        if (media is null) return NotFound();
        var playlistPath = ResolveHlsPath(media, "index.m3u8");
        return System.IO.File.Exists(playlistPath)
            ? PhysicalFile(playlistPath, "application/vnd.apple.mpegurl")
            : NotFound();
    }

    [HttpGet("{id:guid}/hls/{segmentName}")]
    [Authorize(Roles = "ADMINISTRATOR,CREATOR,APPROVER")]
    public async Task<IActionResult> GetHlsSegment(Guid id, string segmentName, CancellationToken cancellationToken)
    {
        if (!HlsSegmentName.IsMatch(segmentName)) return NotFound();
        var media = await GetPlayableHlsMediaAsync(id, cancellationToken);
        if (media is null) return NotFound();
        var segmentPath = ResolveHlsPath(media, segmentName);
        return System.IO.File.Exists(segmentPath)
            ? PhysicalFile(segmentPath, "video/mp2t", enableRangeProcessing: true)
            : NotFound();
    }

    private async Task<Media?> GetPlayableHlsMediaAsync(Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetScope(out var organizationId, out var facilityId)) return null;
        var media = await _context.Media.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);
        if (media is null || media.HlsStatus != "Ready" || media.ApprovalStatus != "\u0110\u00e3 duy\u1ec7t") return null;
        if (media.OrganizationId != organizationId ||
            !await _service.CanAccessFacilityAsync(organizationId, facilityId, media.FacilityId, cancellationToken)) return null;
        return media;
    }

    private string ResolveHlsPath(Media media, string fileName)
    {
        var attemptPath = media.HlsRelativePath;
        if (attemptPath is null || !Regex.IsMatch(attemptPath,
                $"^hls/{media.Id:N}/[0-9a-f]{{32}}/index\\.m3u8$", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase))
            return Path.Combine(_environment.ContentRootPath, "__invalid_hls_path__");
        try
        {
            var root = MediaStoragePathResolver.ResolveStorageRoot(_configuration, _environment);
            var relativeDirectory = Path.GetDirectoryName(attemptPath.Replace('/', Path.DirectorySeparatorChar))!;
            return MediaStoragePathResolver.ResolveUnderRoot(root, Path.Combine(relativeDirectory, fileName));
        }
        catch (InvalidDataException)
        {
            return Path.Combine(_environment.ContentRootPath, "__invalid_hls_path__");
        }
    }

    /// <summary>
    /// Phát / tải trực tiếp luồng nội dung tệp Media (hỗ trợ HTTP Range requests cho trình phát media của thiết bị)
    /// Yêu cầu xác thực: User Bearer JWT (kiểm tra tenant isolation) hoặc DeviceBearer JWT (kiểm tra thiết bị cùng tổ chức)
    /// </summary>
    [HttpGet("{id:guid}/stream")]
    [Authorize(AuthenticationSchemes = $"{JwtBearerDefaults.AuthenticationScheme},DeviceBearer")]
    public async Task<IActionResult> StreamMedia(Guid id, CancellationToken cancellationToken)
    {
        var media = await _context.Media.FirstOrDefaultAsync(m => m.Id == id && !m.IsDeleted, cancellationToken);
        if (media == null)
            return NotFound(ApiResponse<string>.Fail("Không tìm thấy tệp media."));

        // Kiểm tra phân quyền truy cập Media: Ngăn chặn tuyệt đối truy cập chéo tổ chức (Cross-tenant data leak)
        var tokenVersionClaim = User.FindFirstValue("token_version");
        if (!string.IsNullOrEmpty(tokenVersionClaim))
        {
            // Thiết bị (DeviceBearer JWT)
            var deviceIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(deviceIdClaim, out var deviceId))
                return Forbid();

            var device = await _context.Devices.AsNoTracking().FirstOrDefaultAsync(d => d.Id == deviceId && !d.IsDeleted, cancellationToken);
            if (device == null || !device.ActiveStatus || !device.IsActivated)
                return Forbid();

            if (device.OrganizationId != media.OrganizationId)
                return Forbid();

            // Kiểm tra phân quyền truy cập Media: Phân cấp cơ sở hoặc được phân công nhận bản tin chứa media
            bool canAccessHierarchy = await _service.CanAccessFacilityAsync(device.OrganizationId, media.FacilityId, device.FacilityId, cancellationToken);
            if (!canAccessHierarchy)
            {
                // Kiểm tra xem thiết bị có được phân công bản tin nào (trực tiếp qua MediaId hoặc qua Playlist) chứa media này không
                bool isAssigned = await _context.BroadcastDevices
                    .AsNoTracking()
                    .Where(bd => bd.DeviceId == device.Id && !bd.IsDeleted)
                    .Join(_context.BroadcastSchedules.Where(s => !s.IsDeleted),
                        bd => bd.BroadcastScheduleId,
                        s => s.Id,
                        (bd, s) => s.BroadcastId)
                    .Join(_context.Broadcasts.Where(b => !b.IsDeleted),
                        broadcastId => broadcastId,
                        b => b.Id,
                        (broadcastId, b) => b)
                    .AnyAsync(b => b.MediaId == media.Id ||
                        (_context.PlaylistItems.Any(pi => pi.PlaylistId == b.PlaylistId && pi.MediaId == media.Id && !pi.IsDeleted)),
                        cancellationToken);

                if (!isAssigned)
                    return Forbid();
            }

            if (media.ApprovalStatus != "\u0110\u00e3 duy\u1ec7t")
                return Forbid();
        }
        else
        {
            // Người dùng hệ thống (User JWT)
            if (!User.IsInRole("ADMINISTRATOR") && !User.IsInRole("CREATOR") && !User.IsInRole("APPROVER")) return Forbid();
            if (!TryGetScope(out var organizationId, out var facilityId))
                return Forbid();

            if (organizationId != media.OrganizationId)
                return Forbid();

            if (!await _service.CanAccessFacilityAsync(organizationId, facilityId, media.FacilityId, cancellationToken))
                return Forbid();

            if (media.ApprovalStatus != "\u0110\u00e3 duy\u1ec7t" &&
                !(media.ApprovalStatus == "Ch\u1edd duy\u1ec7t" && CanPreviewPendingMedia(media)))
                return Forbid();
        }

        if (!string.IsNullOrWhiteSpace(media.Url))
        {
            return Redirect(media.Url);
        }

        if (string.IsNullOrWhiteSpace(media.FilePath))
        {
            return NotFound(ApiResponse<string>.Fail("Media không có đường dẫn tệp."));
        }

        string fullPath;
        try
        {
            var storageRoot = MediaStoragePathResolver.ResolveStorageRoot(_configuration, _environment);
            fullPath = MediaStoragePathResolver.ResolveUnderRoot(storageRoot, media.FilePath);
        }
        catch (InvalidDataException)
        {
            return NotFound(ApiResponse<string>.Fail("Stored media path is invalid."));
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

    private bool CanPreviewPendingMedia(Media media)
    {
        if (User.IsInRole("APPROVER") || User.IsInRole("ADMINISTRATOR"))
            return true;
        return User.IsInRole("CREATOR") && TryGetUserId(out var userId) && media.CreatedBy == userId;
    }

    [HttpGet]
    [Authorize(Roles = "ADMINISTRATOR,CREATOR,APPROVER")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<MediaResponse>>>> GetAll(CancellationToken cancellationToken)
    {
        if (!TryGetScope(out var organizationId, out var facilityId)) return Forbid();
        var data = await _service.GetAllAsync(organizationId, facilityId, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<MediaResponse>>.Ok(data));
    }

    [HttpGet("{id:guid}")]
    [Authorize(Roles = "ADMINISTRATOR,CREATOR,APPROVER")]
    public async Task<ActionResult<ApiResponse<MediaResponse>>> GetById(Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetScope(out var organizationId, out var facilityId)) return Forbid();
        var data = await _service.GetByIdAsync(id, organizationId, facilityId, cancellationToken);
        return Ok(ApiResponse<MediaResponse>.Ok(data));
    }

    [HttpPost]
    [Authorize(Roles = "ADMINISTRATOR,CREATOR")]
    public async Task<ActionResult<ApiResponse<MediaResponse>>> Create([FromBody] CreateMediaRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetScope(out var organizationId, out var facilityId) || !TryGetUserId(out var userId)) return Forbid();
        if (!await _service.CanAccessFacilityAsync(organizationId, facilityId, request.FacilityId, cancellationToken)) return Forbid();
        var targetFacilityId = request.FacilityId;
        var data = await _service.CreateAsync(request, organizationId, targetFacilityId, userId, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = data.Id }, ApiResponse<MediaResponse>.Ok(data, "Tạo media thành công; nguồn URL chỉ được lưu, không tải về máy chủ."));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "ADMINISTRATOR,CREATOR")]
    public async Task<ActionResult<ApiResponse<MediaResponse>>> Update(Guid id, [FromBody] UpdateMediaRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetScope(out var organizationId, out var facilityId) || !TryGetUserId(out var userId)) return Forbid();
        var data = await _service.UpdateAsync(id, request, organizationId, facilityId, userId, cancellationToken);
        return Ok(ApiResponse<MediaResponse>.Ok(data, "Cập nhật media thành công."));
    }

    [HttpPost("{id:guid}/approve")]
    [Authorize(Roles = "ADMINISTRATOR,APPROVER")]
    public async Task<ActionResult<ApiResponse<MediaResponse>>> Approve(Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetScope(out var organizationId, out var facilityId) || !TryGetUserId(out var userId)) return Forbid();
        var data = await _service.ApproveAsync(id, organizationId, facilityId, userId, cancellationToken);
        return Ok(ApiResponse<MediaResponse>.Ok(data, "Duyệt media thành công."));
    }

    [HttpPost("{id:guid}/reject")]
    [Authorize(Roles = "ADMINISTRATOR,APPROVER")]
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

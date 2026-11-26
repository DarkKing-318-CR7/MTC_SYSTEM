using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MTC_System.Common;
using MTC_System.Data;
using MTC_System.DTOs.SharedMedia;
using MTC_System.Models;
using MTC_System.Services.Implements;

namespace MTC_System.Controllers;

[ApiController]
[Authorize(Roles = "ADMINISTRATOR,CREATOR,APPROVER")]
[Route("api/shared-media")]
public sealed class SharedMediaController(MyDbContext db) : ControllerBase
{
    private const string Pending = "Chờ tiếp nhận";
    private const string AcceptedStatus = "Đã tiếp nhận";
    private const string Approved = "Đã duyệt";

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<SharedMediaResponse>>>> GetIncoming(CancellationToken ct)
    {
        if (!TryGetScope(out var orgId, out var facilityId) || facilityId is null) return Forbid();
        if (!await HasMediaViewPermissionAsync(ct)) return Forbid();
        var rows = await db.SharedMedia.AsNoTracking()
            .Where(x => !x.IsDeleted && x.OrganizationId == orgId && x.ToFacilityId == facilityId &&
                        (x.Status == Pending || x.Status == AcceptedStatus) &&
                        db.Facilities.Any(source => source.Id == x.FromFacilityId && !source.IsDeleted && source.IsActive && source.OrganizationId == orgId) &&
                        db.Facilities.Any(target => target.Id == x.ToFacilityId && !target.IsDeleted && target.IsActive && target.OrganizationId == orgId && target.ParentId == x.FromFacilityId))
            .Join(db.Media.Where(x => !x.IsDeleted && x.ApprovalStatus == Approved), x => x.MediaId, m => m.Id, (x,m) => new { x,m })
            .Where(x => x.m.OrganizationId == orgId && x.m.FacilityId == x.x.FromFacilityId)
            .OrderByDescending(x => x.x.SharedAt)
            .Select(x => new SharedMediaResponse(x.x.Id, x.m.Id, x.m.MediaName, x.m.FileName, x.m.FileType,
                x.m.FileSize, x.m.DurationSeconds, x.m.ApprovalStatus, x.m.HlsStatus,
                x.m.HlsStatus == "Ready" ? $"/api/media/{x.m.Id}/hls/index.m3u8" : null,
                db.MediaCategories.Where(c => c.Id == x.m.CategoryId && !c.IsDeleted).Select(c => c.CategoryName).FirstOrDefault(),
                db.Facilities.Where(f => f.Id == x.x.FromFacilityId && !f.IsDeleted).Select(f => f.FacilityName).FirstOrDefault() ?? "",
                db.Facilities.Where(f => f.Id == x.x.ToFacilityId && !f.IsDeleted).Select(f => f.FacilityName).FirstOrDefault() ?? "",
                db.Users.Where(u => u.Id == x.m.CreatedBy && !u.IsDeleted).Select(u => u.FullName).FirstOrDefault(),
                db.Users.Where(u => u.Id == x.m.ApprovedBy && !u.IsDeleted).Select(u => u.FullName).FirstOrDefault(),
                x.m.CreatedAt, x.x.SharedAt, x.x.Status, x.x.RespondedAt))
            .ToListAsync(ct);
        return Ok(ApiResponse<IReadOnlyList<SharedMediaResponse>>.Ok(rows));
    }

    [HttpPost]
    [Authorize(Roles = "ADMINISTRATOR,CREATOR")]
    public async Task<ActionResult<ApiResponse<SharedMediaResponse>>> Share(CreateSharedMediaRequest request, CancellationToken ct)
    {
        if (!TryGetScope(out var orgId, out var facilityId) || !TryGetUserId(out var userId)) return Forbid();
        var media = await db.Media.SingleOrDefaultAsync(x => x.Id == request.MediaId && !x.IsDeleted && x.OrganizationId == orgId && x.ApprovalStatus == Approved, ct);
        if (media is null) return NotFound();
        if (facilityId.HasValue && facilityId.Value != media.FacilityId) return Forbid();
        if (facilityId is null && !User.IsInRole("ADMINISTRATOR")) return Forbid();
        var recipient = await db.Facilities.SingleOrDefaultAsync(x => x.Id == request.ToFacilityId && !x.IsDeleted && x.IsActive &&
            x.OrganizationId == orgId && x.ParentId == media.FacilityId, ct);
        if (recipient is null) return BadRequest("Media may only be shared with an immediate child facility in the same organization.");

        var existing = await db.SharedMedia.SingleOrDefaultAsync(x => x.FromFacilityId == media.FacilityId && x.ToFacilityId == recipient.Id && x.MediaId == media.Id, ct);
        if (existing is { IsDeleted: false }) return Conflict("An active share already exists.");
        var now = DateTimeOffset.UtcNow;
        if (existing is null)
        {
            existing = new SharedMedia { OrganizationId = orgId, FromFacilityId = media.FacilityId, ToFacilityId = recipient.Id, MediaId = media.Id, CreatedBy = userId };
            db.SharedMedia.Add(existing);
        }
        else
        {
            existing.IsDeleted = false;
            existing.Status = Pending;
            existing.RespondedAt = null;
            existing.RespondedBy = null;
        }
        existing.SharedAt = now;
        existing.UpdatedAt = now;
        await db.SaveChangesAsync(ct);
        return Ok(ApiResponse<SharedMediaResponse>.Ok(await MapAsync(existing.Id, ct)));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<SharedMediaResponse>>> GetById(Guid id, CancellationToken ct)
    {
        if (!TryGetScope(out var orgId, out var facilityId) || facilityId is null) return Forbid();
        if (!await HasMediaViewPermissionAsync(ct)) return Forbid();
        var share = await db.SharedMedia.SingleOrDefaultAsync(x => x.Id == id && !x.IsDeleted && x.OrganizationId == orgId && x.ToFacilityId == facilityId, ct);
        if (share is null || !await IsSourceAvailableAsync(share, ct)) return NotFound();
        return Ok(ApiResponse<SharedMediaResponse>.Ok(await MapAsync(share.Id, ct)));
    }

    [HttpPost("{id:guid}/accept")]
    public async Task<ActionResult<ApiResponse<SharedMediaResponse>>> Accept(Guid id, CancellationToken ct)
    {
        if (!TryGetScope(out var orgId, out var facilityId) || facilityId is null || !TryGetUserId(out var userId)) return Forbid();
        if (!await HasMediaViewPermissionAsync(ct)) return Forbid();
        var share = await db.SharedMedia.SingleOrDefaultAsync(x => x.Id == id && !x.IsDeleted && x.OrganizationId == orgId && x.ToFacilityId == facilityId, ct);
        if (share is null || !await IsSourceAvailableAsync(share, ct)) return NotFound();
        if (share.Status != AcceptedStatus)
        {
            share.Status = AcceptedStatus;
            share.RespondedAt = DateTimeOffset.UtcNow;
            share.RespondedBy = userId;
            share.UpdatedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(ct);
        }
        return Ok(ApiResponse<SharedMediaResponse>.Ok(await MapAsync(share.Id, ct)));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Remove(Guid id, CancellationToken ct)
    {
        if (!TryGetScope(out var orgId, out var facilityId) || facilityId is null) return Forbid();
        if (!await HasMediaViewPermissionAsync(ct)) return Forbid();
        var share = await db.SharedMedia.SingleOrDefaultAsync(x => x.Id == id && !x.IsDeleted && x.OrganizationId == orgId && x.ToFacilityId == facilityId, ct);
        if (share is null) return NotFound();
        share.IsDeleted = true;
        share.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    private async Task<SharedMediaResponse> MapAsync(Guid id, CancellationToken ct)
    {
        var row = await db.SharedMedia.AsNoTracking().Where(x => x.Id == id && !x.IsDeleted)
            .Join(db.Media.Where(m => !m.IsDeleted), x => x.MediaId, m => m.Id, (x,m) => new {x,m})
            .Select(x => new SharedMediaResponse(x.x.Id, x.m.Id, x.m.MediaName, x.m.FileName, x.m.FileType, x.m.FileSize,
                x.m.DurationSeconds, x.m.ApprovalStatus, x.m.HlsStatus, x.m.HlsStatus == "Ready" ? $"/api/media/{x.m.Id}/hls/index.m3u8" : null,
                db.MediaCategories.Where(c => c.Id == x.m.CategoryId && !c.IsDeleted).Select(c => c.CategoryName).FirstOrDefault(),
                db.Facilities.Where(f => f.Id == x.x.FromFacilityId && !f.IsDeleted).Select(f => f.FacilityName).FirstOrDefault() ?? "",
                db.Facilities.Where(f => f.Id == x.x.ToFacilityId && !f.IsDeleted).Select(f => f.FacilityName).FirstOrDefault() ?? "",
                db.Users.Where(u => u.Id == x.m.CreatedBy && !u.IsDeleted).Select(u => u.FullName).FirstOrDefault(),
                db.Users.Where(u => u.Id == x.m.ApprovedBy && !u.IsDeleted).Select(u => u.FullName).FirstOrDefault(), x.m.CreatedAt, x.x.SharedAt, x.x.Status, x.x.RespondedAt))
            .SingleAsync(ct);
        return row;
    }

    private async Task<bool> IsSourceAvailableAsync(SharedMedia share, CancellationToken ct) =>
        await db.Media.AnyAsync(m => m.Id == share.MediaId && !m.IsDeleted && m.OrganizationId == share.OrganizationId && m.ApprovalStatus == Approved, ct) &&
        await db.Facilities.AnyAsync(f => f.Id == share.FromFacilityId && !f.IsDeleted && f.IsActive && f.OrganizationId == share.OrganizationId, ct) &&
        await db.Facilities.AnyAsync(f => f.Id == share.ToFacilityId && !f.IsDeleted && f.IsActive && f.OrganizationId == share.OrganizationId && f.ParentId == share.FromFacilityId, ct);

    private async Task<bool> HasMediaViewPermissionAsync(CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return false;
        return await (from ur in db.UserRoles.Where(x => x.UserId == userId && !x.IsDeleted)
                      join rp in db.RolePermissions.Where(x => !x.IsDeleted) on ur.RoleId equals rp.RoleId
                      join p in db.Permissions.Where(x => !x.IsDeleted && x.PermissionCode == "MEDIA_VIEW") on rp.PermissionId equals p.Id
                      select ur.Id).AnyAsync(ct);
    }

    private bool TryGetScope(out Guid orgId, out Guid? facilityId)
    {
        facilityId = null;
        if (!Guid.TryParse(User.FindFirstValue("organizationId"), out orgId)) return false;
        var facility = User.FindFirstValue("facilityId");
        if (string.IsNullOrEmpty(facility)) return true;
        if (!Guid.TryParse(facility, out var parsed)) return false;
        facilityId = parsed;
        return true;
    }

    private bool TryGetUserId(out Guid id) => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out id);
}

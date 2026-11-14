using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using MTC_System.Data;
using MTC_System.DTOs.Media;
using MTC_System.Models;
using MTC_System.Services.Interfaces;

namespace MTC_System.Services.Implements;

public sealed class MediaService : IMediaService
{
    private const string Pending = "Chờ duyệt";
    private const string Approved = "Đã duyệt";
    private const string Rejected = "Từ chối duyệt";
    private readonly MyDbContext _db;

    public MediaService(MyDbContext db) => _db = db;

    public async Task<bool> CanAccessFacilityAsync(Guid organizationId, Guid? rootFacilityId, Guid targetFacilityId, CancellationToken cancellationToken)
    {
        var scope = await ResolveFacilityScopeAsync(organizationId, rootFacilityId, cancellationToken);
        return scope == null || scope.Contains(targetFacilityId);
    }

    public async Task<IReadOnlyList<MediaResponse>> GetAllAsync(Guid organizationId, Guid? facilityId, CancellationToken cancellationToken)
    {
        var facilityScope = await ResolveFacilityScopeAsync(organizationId, facilityId, cancellationToken);
        var query = Scope(_db.Media.AsNoTracking().Where(x => !x.IsDeleted), organizationId, facilityScope);
        var media = await query.OrderByDescending(x => x.CreatedAt).ToListAsync(cancellationToken);
        return media.Select(Map).ToList();
    }

    public async Task<MediaResponse> GetByIdAsync(Guid id, Guid organizationId, Guid? facilityId, CancellationToken cancellationToken)
    {
        var facilityScope = await ResolveFacilityScopeAsync(organizationId, facilityId, cancellationToken);
        var media = await Scope(_db.Media.AsNoTracking().Where(x => x.Id == id && !x.IsDeleted), organizationId, facilityScope)
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw Error(StatusCodes.Status404NotFound, "Không tìm thấy media.");
        return Map(media);
    }

    public async Task<MediaResponse> CreateAsync(CreateMediaRequest request, Guid organizationId, Guid facilityId, Guid userId, CancellationToken cancellationToken)
    {
        var sourceType = request.SourceType!.Value;
        ValidateSource(sourceType, request.FileType, request.SourceUrl, request.Url, request.FrequencyHz);
        if (sourceType == MediaSourceMethod.File)
            throw Error(StatusCodes.Status400BadRequest, "Tệp media phải được tạo qua luồng tải tệp hiện có.");
        await EnsureFacilityAsync(facilityId, organizationId, null, cancellationToken);
        await EnsureCategoryAsync(request.CategoryId, organizationId, cancellationToken);

        var media = new Media
        {
            MediaName = request.MediaName.Trim(),
            FileName = string.Empty,
            FilePath = string.Empty,
            FileType = request.FileType.Trim(),
            SourceType = sourceType,
            SourceUrl = sourceType == MediaSourceMethod.UrlDownload ? request.SourceUrl!.Trim() : null,
            Url = sourceType == MediaSourceMethod.StreamUrl ? request.Url!.Trim() : null,
            FrequencyHz = sourceType == MediaSourceMethod.FrequencyChannel ? request.FrequencyHz : null,
            ApprovalStatus = Pending,
            OrganizationId = organizationId,
            FacilityId = facilityId,
            CategoryId = request.CategoryId,
            CreatedBy = userId
        };
        _db.Media.Add(media);
        await _db.SaveChangesAsync(cancellationToken);
        return Map(media);
    }

    public async Task<MediaResponse> UpdateAsync(Guid id, UpdateMediaRequest request, Guid organizationId, Guid? facilityId, Guid userId, CancellationToken cancellationToken)
    {
        var facilityScope = await ResolveFacilityScopeAsync(organizationId, facilityId, cancellationToken);
        var media = await Scope(_db.Media.Where(x => x.Id == id && !x.IsDeleted), organizationId, facilityScope)
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw Error(StatusCodes.Status404NotFound, "Không tìm thấy media.");
        if (media.ApprovalStatus != Pending)
            throw Error(StatusCodes.Status409Conflict, "Chỉ media chờ duyệt mới được chỉnh sửa.");
        var sourceType = request.SourceType!.Value;
        ValidateSource(sourceType, request.FileType, request.SourceUrl, request.Url, request.FrequencyHz);
        if (sourceType != media.SourceType)
            throw Error(StatusCodes.Status400BadRequest, "Không thể thay đổi phương thức nguồn của media hiện có.");
        if (sourceType == MediaSourceMethod.File && !string.Equals(request.FileType.Trim(), media.FileType, StringComparison.OrdinalIgnoreCase))
            throw Error(StatusCodes.Status400BadRequest, "Không thể thay đổi loại nội dung của tệp đã tải lên.");
        await EnsureFacilityAsync(request.FacilityId, organizationId, facilityScope, cancellationToken);
        await EnsureCategoryAsync(request.CategoryId, organizationId, cancellationToken);

        var now = DateTimeOffset.UtcNow;
        var mediaName = request.MediaName.Trim();
        var fileType = request.FileType.Trim();
        var sourceUrl = sourceType == MediaSourceMethod.UrlDownload ? request.SourceUrl!.Trim() : null;
        var url = sourceType == MediaSourceMethod.StreamUrl ? request.Url!.Trim() : null;
        var frequencyHz = sourceType == MediaSourceMethod.FrequencyChannel ? request.FrequencyHz : null;
        var changed = await Scope(_db.Media.Where(x => x.Id == id && !x.IsDeleted && x.ApprovalStatus == Pending), organizationId, facilityScope)
            .ExecuteUpdateAsync(updates => updates
                .SetProperty(x => x.MediaName, mediaName)
                .SetProperty(x => x.FileType, fileType)
                .SetProperty(x => x.SourceUrl, sourceUrl)
                .SetProperty(x => x.Url, url)
                .SetProperty(x => x.FrequencyHz, frequencyHz)
                .SetProperty(x => x.CategoryId, request.CategoryId)
                .SetProperty(x => x.FacilityId, request.FacilityId)
                .SetProperty(x => x.UpdatedAt, now)
                .SetProperty(x => x.UpdatedBy, userId), cancellationToken);
        if (changed != 1)
            throw Error(StatusCodes.Status409Conflict, "Media đã được duyệt hoặc không còn chờ duyệt.");

        media.MediaName = mediaName;
        media.FileType = fileType;
        media.SourceUrl = sourceUrl;
        media.Url = url;
        media.FrequencyHz = frequencyHz;
        media.CategoryId = request.CategoryId;
        media.FacilityId = request.FacilityId;
        media.UpdatedAt = now;
        media.UpdatedBy = userId;
        return Map(media);
    }

    public Task<MediaResponse> ApproveAsync(Guid id, Guid organizationId, Guid? facilityId, Guid checkerId, CancellationToken cancellationToken) =>
        ReviewAsync(id, organizationId, facilityId, checkerId, approve: true, reason: null, cancellationToken);

    public Task<MediaResponse> RejectAsync(Guid id, string? reason, Guid organizationId, Guid? facilityId, Guid checkerId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length > 510)
            throw Error(StatusCodes.Status400BadRequest, "Lý do từ chối bắt buộc và tối đa 510 ký tự.");
        return ReviewAsync(id, organizationId, facilityId, checkerId, approve: false, reason: reason.Trim(), cancellationToken);
    }

    private async Task<MediaResponse> ReviewAsync(Guid id, Guid organizationId, Guid? facilityId, Guid checkerId, bool approve, string? reason, CancellationToken cancellationToken)
    {
        var facilityScope = await ResolveFacilityScopeAsync(organizationId, facilityId, cancellationToken);
        var media = await Scope(_db.Media.AsNoTracking().Where(x => x.Id == id && !x.IsDeleted), organizationId, facilityScope)
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw Error(StatusCodes.Status404NotFound, "Không tìm thấy media.");
        if (media.ApprovalStatus != Pending)
            throw Error(StatusCodes.Status409Conflict, "Chỉ media chờ duyệt mới được duyệt hoặc từ chối.");
        if (!media.CreatedBy.HasValue)
            throw Error(StatusCodes.Status409Conflict, "Không thể xác minh người tạo media để kiểm tra phân tách nhiệm vụ.");
        if (media.CreatedBy == checkerId)
            throw Error(StatusCodes.Status403Forbidden, "Người tạo media không được tự duyệt hoặc từ chối media của mình.");

        var now = DateTimeOffset.UtcNow;
        var changed = await Scope(_db.Media.Where(x => x.Id == id && !x.IsDeleted && x.ApprovalStatus == Pending), organizationId, facilityScope)
            .ExecuteUpdateAsync(updates => updates
                .SetProperty(x => x.ApprovalStatus, approve ? Approved : Rejected)
                .SetProperty(x => x.RejectionReason, approve ? null : reason)
                .SetProperty(x => x.RejectedAt, approve ? null : now.UtcDateTime)
                .SetProperty(x => x.RejectedBy, approve ? null : checkerId)
                .SetProperty(x => x.UpdatedAt, now)
                .SetProperty(x => x.UpdatedBy, checkerId), cancellationToken);
        if (changed != 1)
            throw Error(StatusCodes.Status409Conflict, "Media đã được xử lý hoặc không còn chờ duyệt.");

        media.ApprovalStatus = approve ? Approved : Rejected;
        media.RejectionReason = approve ? null : reason;
        media.RejectedAt = approve ? null : now.UtcDateTime;
        media.RejectedBy = approve ? null : checkerId;
        media.UpdatedAt = now;
        media.UpdatedBy = checkerId;
        return Map(media);
    }

    private async Task EnsureFacilityAsync(Guid facilityId, Guid organizationId, IReadOnlyCollection<Guid>? facilityScope, CancellationToken cancellationToken)
    {
        var exists = await _db.Facilities.AnyAsync(x => x.Id == facilityId && x.OrganizationId == organizationId && x.IsActive && !x.IsDeleted &&
            (facilityScope == null || facilityScope.Contains(x.Id)), cancellationToken);
        if (!exists) throw Error(StatusCodes.Status400BadRequest, "Cơ sở không tồn tại, đã ngừng hoạt động hoặc không thuộc tổ chức.");
    }

    private async Task<IReadOnlyCollection<Guid>?> ResolveFacilityScopeAsync(Guid organizationId, Guid? rootFacilityId, CancellationToken cancellationToken)
    {
        if (!rootFacilityId.HasValue) return null;
        var facilities = await _db.Facilities.AsNoTracking()
            .Where(x => x.OrganizationId == organizationId && x.IsActive && !x.IsDeleted)
            .Select(x => new { x.Id, x.ParentId })
            .ToListAsync(cancellationToken);
        var children = facilities.ToLookup(x => x.ParentId, x => x.Id);
        if (!facilities.Any(x => x.Id == rootFacilityId.Value))
            throw Error(StatusCodes.Status403Forbidden, "Cơ sở của người dùng không tồn tại, đã ngừng hoạt động hoặc không thuộc tổ chức.");

        var scope = new HashSet<Guid> { rootFacilityId.Value };
        var pending = new Queue<Guid>();
        pending.Enqueue(rootFacilityId.Value);
        while (pending.TryDequeue(out var parentId))
        {
            foreach (var childId in children[parentId])
                if (scope.Add(childId)) pending.Enqueue(childId);
        }
        return scope;
    }

    private async Task EnsureCategoryAsync(Guid? categoryId, Guid organizationId, CancellationToken cancellationToken)
    {
        if (!categoryId.HasValue) return;
        var exists = await _db.MediaCategories.AnyAsync(x => x.Id == categoryId && !x.IsDeleted && (x.OrganizationId == null || x.OrganizationId == organizationId), cancellationToken);
        if (!exists) throw Error(StatusCodes.Status400BadRequest, "Lĩnh vực không khả dụng cho tổ chức.");
    }

    private static IQueryable<Media> Scope(IQueryable<Media> query, Guid organizationId, IReadOnlyCollection<Guid>? facilityScope) =>
        query.Where(x => x.OrganizationId == organizationId && (facilityScope == null || facilityScope.Contains(x.FacilityId)));

    private static void ValidateSource(MediaSourceMethod sourceType, string fileType, string? sourceUrl, string? url, decimal? frequencyHz)
    {
        var normalizedType = fileType.Trim();
        var isFileContent = normalizedType.Equals("Image", StringComparison.OrdinalIgnoreCase) ||
                            normalizedType.Equals("Audio", StringComparison.OrdinalIgnoreCase) ||
                            normalizedType.Equals("Video", StringComparison.OrdinalIgnoreCase);
        var valid = sourceType switch
        {
            MediaSourceMethod.File => isFileContent && string.IsNullOrWhiteSpace(sourceUrl) && string.IsNullOrWhiteSpace(url) && frequencyHz is null,
            MediaSourceMethod.UrlDownload => isFileContent && IsAbsoluteUrl(sourceUrl) && string.IsNullOrWhiteSpace(url) && frequencyHz is null,
            MediaSourceMethod.StreamUrl => normalizedType.Equals("LiveStream", StringComparison.OrdinalIgnoreCase) && IsAbsoluteUrl(url) && string.IsNullOrWhiteSpace(sourceUrl) && frequencyHz is null,
            MediaSourceMethod.FrequencyChannel => normalizedType.Equals("Frequency", StringComparison.OrdinalIgnoreCase) && frequencyHz > 0 && string.IsNullOrWhiteSpace(sourceUrl) && string.IsNullOrWhiteSpace(url),
            _ => false
        };
        if (!valid)
            throw Error(StatusCodes.Status400BadRequest, "Các trường media không phù hợp với phương thức nguồn đã chọn.");
    }

    private static bool IsAbsoluteUrl(string? value) =>
        Uri.TryCreate(value, UriKind.Absolute, out var uri) &&
        (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);

    private static MediaResponse Map(Media media) => new()
    {
        Id = media.Id,
        MediaName = media.MediaName,
        FileName = media.FileName,
        FileType = media.FileType,
        SourceType = media.SourceType,
        FileSize = media.FileSize,
        DurationSeconds = media.DurationSeconds,
        HlsProcessingStatus = media.HlsStatus,
        HlsPlaybackUrl = media.HlsStatus == "Ready" ? $"/api/media/{media.Id:D}/hls/index.m3u8" : null,
        ApprovalStatus = media.ApprovalStatus,
        RejectionReason = media.RejectionReason,
        Url = media.Url,
        SourceUrl = media.SourceUrl,
        FrequencyHz = media.FrequencyHz,
        CategoryId = media.CategoryId,
        OrganizationId = media.OrganizationId,
        FacilityId = media.FacilityId,
        CreatedBy = media.CreatedBy,
        CreatedAt = media.CreatedAt,
        UpdatedAt = media.UpdatedAt
    };

    private static MediaModuleException Error(int statusCode, string message) => new(statusCode, message);
}

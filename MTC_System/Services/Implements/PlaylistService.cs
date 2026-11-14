using Microsoft.EntityFrameworkCore;
using MTC_System.Common;
using MTC_System.Data;
using MTC_System.DTOs.Playlists;
using MTC_System.Models;
using MTC_System.Services.Interfaces;

namespace MTC_System.Services.Implements;

public sealed class PlaylistService : IPlaylistService
{
    private const string Approved = "Đã duyệt";
    private static readonly string[] PlayModes = ["Theo thứ tự", "Ngẫu nhiên"];
    private readonly MyDbContext _db;

    public PlaylistService(MyDbContext db) => _db = db;

    public async Task<IReadOnlyList<PlaylistResponse>> GetAllAsync(
        UserScopeContext userScope,
        Guid? organizationId,
        Guid? facilityId,
        CancellationToken cancellationToken)
    {
        userScope.EnsureCanAccess();
        if (!userScope.IsSystemAdministrator)
        {
            if (organizationId.HasValue && organizationId != userScope.OrganizationId)
                throw DeviceModuleException.Forbidden("Không được phép truy vấn dữ liệu của tổ chức khác.");
            organizationId = userScope.OrganizationId;
        }

        var query = _db.Playlists.AsNoTracking().Where(x => !x.IsDeleted);
        if (organizationId.HasValue)
            query = query.Where(x => x.OrganizationId == organizationId.Value);
        if (facilityId.HasValue)
            query = query.Where(x => x.FacilityId == facilityId.Value);
        if (!userScope.IsSystemAdministrator && userScope.FacilityId.HasValue)
        {
            var facilityScope = await FacilityScopeResolver.ResolveAsync(_db, userScope, organizationId!.Value, cancellationToken);
            if (facilityId.HasValue && facilityScope is not null && !facilityScope.Contains(facilityId.Value))
                throw DeviceModuleException.Forbidden("Không được phép truy cập dữ liệu của cơ sở này.");
            if (facilityScope is not null)
                query = query.Where(x => facilityScope.Contains(x.FacilityId));
        }

        var playlists = await query.Include(x => x.Items.Where(i => !i.IsDeleted))
            .OrderBy(x => x.PlaylistName)
            .ToListAsync(cancellationToken);
        return await MapManyAsync(playlists, cancellationToken);
    }

    public async Task<PlaylistResponse> GetByIdAsync(Guid id, UserScopeContext userScope, CancellationToken cancellationToken)
    {
        var playlist = await FindAsync(id, cancellationToken);
        await EnsureCanAccessAsync(userScope, playlist.OrganizationId, playlist.FacilityId, cancellationToken);
        return (await MapManyAsync([playlist], cancellationToken)).Single();
    }

    public async Task<PlaylistResponse> CreateAsync(PlaylistUpsertRequest request, UserScopeContext userScope, CancellationToken cancellationToken)
    {
        var organizationId = ResolveOrganizationId(userScope, request.OrganizationId);
        var facilityScope = await FacilityScopeResolver.ResolveAsync(_db, userScope, organizationId, cancellationToken);
        ValidatePlaylistFields(request.PlaylistName, request.Description, request.PlayMode, request.Items);
        await FacilityScopeResolver.ValidateFacilityAsync(_db, userScope, organizationId, request.FacilityId, facilityScope, cancellationToken);
        var media = await ValidateMediaAsync(request.Items, organizationId, request.FacilityId, cancellationToken);

        var playlist = new Playlist
        {
            OrganizationId = organizationId,
            FacilityId = request.FacilityId,
            PlaylistName = request.PlaylistName.Trim(),
            Description = request.Description?.Trim(),
            PlayMode = NormalizePlayMode(request.PlayMode),
            IsActive = true,
            CreatedBy = userScope.UserId
        };
        ApplyItems(playlist, [], request.Items, media, userScope.UserId);
        _db.Playlists.Add(playlist);
        await _db.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(playlist.Id, userScope, cancellationToken);
    }

    public async Task<PlaylistResponse> UpdateAsync(Guid id, PlaylistUpsertRequest request, UserScopeContext userScope, CancellationToken cancellationToken)
    {
        var playlist = await FindAsync(id, cancellationToken);
        await EnsureCanAccessAsync(userScope, playlist.OrganizationId, playlist.FacilityId, cancellationToken);
        await EnsureNotInUseAsync(id, cancellationToken);
        if (request.OrganizationId.HasValue && request.OrganizationId.Value != playlist.OrganizationId)
            throw DeviceModuleException.BadRequest("Không được phép thay đổi tổ chức của Playlist.");
        ValidatePlaylistFields(request.PlaylistName, request.Description, request.PlayMode, request.Items);
        var facilityScope = await FacilityScopeResolver.ResolveAsync(_db, userScope, playlist.OrganizationId, cancellationToken);
        await FacilityScopeResolver.ValidateFacilityAsync(_db, userScope, playlist.OrganizationId, request.FacilityId, facilityScope, cancellationToken);
        var media = await ValidateMediaAsync(request.Items, playlist.OrganizationId, request.FacilityId, cancellationToken);

        playlist.FacilityId = request.FacilityId;
        playlist.PlaylistName = request.PlaylistName.Trim();
        playlist.Description = request.Description?.Trim();
        playlist.PlayMode = NormalizePlayMode(request.PlayMode);
        playlist.UpdatedAt = DateTimeOffset.UtcNow;
        playlist.UpdatedBy = userScope.UserId;
        ApplyItems(playlist, playlist.Items.ToList(), request.Items, media, userScope.UserId);
        await _db.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(id, userScope, cancellationToken);
    }

    public async Task<IReadOnlyList<PlaylistItemResponse>> GetItemsAsync(Guid id, UserScopeContext userScope, CancellationToken cancellationToken) =>
        (await GetByIdAsync(id, userScope, cancellationToken)).Items;

    public async Task<IReadOnlyList<PlaylistItemResponse>> ReplaceItemsAsync(
        Guid id,
        IReadOnlyList<PlaylistItemUpsertRequest> items,
        UserScopeContext userScope,
        CancellationToken cancellationToken)
    {
        var playlist = await FindAsync(id, cancellationToken);
        await EnsureCanAccessAsync(userScope, playlist.OrganizationId, playlist.FacilityId, cancellationToken);
        await EnsureNotInUseAsync(id, cancellationToken);
        ValidateItems(items);
        var media = await ValidateMediaAsync(items, playlist.OrganizationId, playlist.FacilityId, cancellationToken);
        var allItems = await _db.PlaylistItems.Where(x => x.PlaylistId == id).ToListAsync(cancellationToken);
        ApplyItems(playlist, allItems, items, media, userScope.UserId);
        playlist.TotalDurationSeconds = CalculateTotalDuration(items, media);
        playlist.UpdatedAt = DateTimeOffset.UtcNow;
        playlist.UpdatedBy = userScope.UserId;
        await _db.SaveChangesAsync(cancellationToken);
        return await GetItemsAsync(id, userScope, cancellationToken);
    }

    public async Task<PlaylistItemResponse> AddItemAsync(
        Guid id,
        PlaylistItemUpsertRequest item,
        UserScopeContext userScope,
        CancellationToken cancellationToken)
    {
        var playlist = await FindAsync(id, cancellationToken);
        await EnsureCanAccessAsync(userScope, playlist.OrganizationId, playlist.FacilityId, cancellationToken);
        await EnsureNotInUseAsync(id, cancellationToken);
        var existing = await _db.PlaylistItems.AnyAsync(x => x.PlaylistId == id && x.MediaId == item.MediaId && !x.IsDeleted, cancellationToken);
        if (existing)
            throw DeviceModuleException.Conflict("Media đã tồn tại trong Playlist.");
        var items = playlist.Items.OrderBy(x => x.DisplayOrder)
            .Select(x => new PlaylistItemUpsertRequest { MediaId = x.MediaId, DurationSeconds = x.DurationSeconds })
            .Append(item).ToList();
        ValidateItems(items);
        var media = await ValidateMediaAsync(items, playlist.OrganizationId, playlist.FacilityId, cancellationToken);
        var allItems = await _db.PlaylistItems.Where(x => x.PlaylistId == id).ToListAsync(cancellationToken);
        ApplyItems(playlist, allItems, items, media, userScope.UserId);
        playlist.TotalDurationSeconds = CalculateTotalDuration(items, media);
        playlist.UpdatedAt = DateTimeOffset.UtcNow;
        playlist.UpdatedBy = userScope.UserId;
        await _db.SaveChangesAsync(cancellationToken);
        var response = await GetByIdAsync(id, userScope, cancellationToken);
        return response.Items.Single(x => x.MediaId == item.MediaId);
    }

    public async Task RemoveItemAsync(Guid id, Guid mediaId, UserScopeContext userScope, CancellationToken cancellationToken)
    {
        var playlist = await FindAsync(id, cancellationToken);
        await EnsureCanAccessAsync(userScope, playlist.OrganizationId, playlist.FacilityId, cancellationToken);
        await EnsureNotInUseAsync(id, cancellationToken);
        var item = await _db.PlaylistItems.FirstOrDefaultAsync(x => x.PlaylistId == id && x.MediaId == mediaId && !x.IsDeleted, cancellationToken)
            ?? throw DeviceModuleException.NotFound("Không tìm thấy Media trong Playlist.");
        item.IsDeleted = true;
        item.UpdatedAt = DateTimeOffset.UtcNow;
        item.UpdatedBy = userScope.UserId;
        var remaining = playlist.Items.Where(x => x.Id != item.Id && !x.IsDeleted).OrderBy(x => x.DisplayOrder).ToList();
        for (var index = 0; index < remaining.Count; index++)
            remaining[index].DisplayOrder = index + 1;
        playlist.TotalDurationSeconds = await CalculateExistingTotalDurationAsync(remaining, cancellationToken);
        playlist.UpdatedAt = DateTimeOffset.UtcNow;
        playlist.UpdatedBy = userScope.UserId;
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, UserScopeContext userScope, CancellationToken cancellationToken)
    {
        var playlist = await FindAsync(id, cancellationToken);
        await EnsureCanAccessAsync(userScope, playlist.OrganizationId, playlist.FacilityId, cancellationToken);
        await EnsureNotInUseAsync(id, cancellationToken);
        var now = DateTimeOffset.UtcNow;
        playlist.IsDeleted = true;
        playlist.UpdatedAt = now;
        playlist.UpdatedBy = userScope.UserId;
        foreach (var item in await _db.PlaylistItems.Where(x => x.PlaylistId == id && !x.IsDeleted).ToListAsync(cancellationToken))
        {
            item.IsDeleted = true;
            item.UpdatedAt = now;
            item.UpdatedBy = userScope.UserId;
        }
        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task<Playlist> FindAsync(Guid id, CancellationToken cancellationToken) =>
        await _db.Playlists.Include(x => x.Items.Where(i => !i.IsDeleted))
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken)
        ?? throw DeviceModuleException.NotFound("Không tìm thấy Playlist.");

    private async Task EnsureCanAccessAsync(UserScopeContext userScope, Guid organizationId, Guid facilityId, CancellationToken cancellationToken)
    {
        var facilityScope = await FacilityScopeResolver.ResolveAsync(_db, userScope, organizationId, cancellationToken);
        if (!userScope.IsSystemAdministrator && facilityScope is not null && !facilityScope.Contains(facilityId))
            throw DeviceModuleException.Forbidden("Không được phép truy cập dữ liệu của cơ sở này.");
    }

    private async Task EnsureNotInUseAsync(Guid id, CancellationToken cancellationToken)
    {
        if (await _db.Broadcasts.AnyAsync(x => x.PlaylistId == id && !x.IsDeleted, cancellationToken))
            throw DeviceModuleException.Conflict("Không thể sửa hoặc xóa Playlist đang được sử dụng bởi Broadcast.");
    }

    private async Task<Dictionary<Guid, Media>> ValidateMediaAsync(
        IReadOnlyList<PlaylistItemUpsertRequest> items,
        Guid organizationId,
        Guid facilityId,
        CancellationToken cancellationToken)
    {
        ValidateItems(items);
        var ids = items.Select(x => x.MediaId).ToArray();
        var media = await _db.Media.Where(x => ids.Contains(x.Id) && !x.IsDeleted && x.OrganizationId == organizationId &&
                x.FacilityId == facilityId && x.ApprovalStatus == Approved)
            .ToDictionaryAsync(x => x.Id, cancellationToken);
        if (media.Count != ids.Length)
            throw DeviceModuleException.BadRequest("Mọi Playlist item phải trỏ đến Media đã duyệt trong cùng tổ chức và cơ sở.");
        return media;
    }

    private async Task<IReadOnlyList<PlaylistResponse>> MapManyAsync(IReadOnlyCollection<Playlist> playlists, CancellationToken cancellationToken)
    {
        var itemIds = playlists.SelectMany(x => x.Items).Where(x => !x.IsDeleted).Select(x => x.MediaId).Distinct().ToArray();
        var media = await _db.Media.AsNoTracking().Where(x => itemIds.Contains(x.Id) && !x.IsDeleted)
            .ToDictionaryAsync(x => x.Id, cancellationToken);
        return playlists.Select(playlist => Map(playlist, media)).ToList();
    }

    private static PlaylistResponse Map(Playlist playlist, IReadOnlyDictionary<Guid, Media> media) => new()
    {
        Id = playlist.Id,
        OrganizationId = playlist.OrganizationId,
        FacilityId = playlist.FacilityId,
        PlaylistName = playlist.PlaylistName,
        Description = playlist.Description,
        PlayMode = playlist.PlayMode,
        TotalDurationSeconds = playlist.TotalDurationSeconds,
        IsActive = playlist.IsActive,
        CreatedAt = playlist.CreatedAt,
        UpdatedAt = playlist.UpdatedAt,
        Items = playlist.Items.Where(x => !x.IsDeleted).OrderBy(x => x.DisplayOrder).Select(item => new PlaylistItemResponse
        {
            Id = item.Id,
            MediaId = item.MediaId,
            MediaName = media.TryGetValue(item.MediaId, out var itemMedia) ? itemMedia.MediaName : string.Empty,
            FileType = media.TryGetValue(item.MediaId, out itemMedia) ? itemMedia.FileType : string.Empty,
            DisplayOrder = item.DisplayOrder,
            DurationSeconds = item.DurationSeconds,
            MediaDurationSeconds = media.TryGetValue(item.MediaId, out itemMedia) ? itemMedia.DurationSeconds : 0
        }).ToList()
    };

    private static void ApplyItems(
        Playlist playlist,
        IReadOnlyCollection<PlaylistItem> allItems,
        IReadOnlyList<PlaylistItemUpsertRequest> requests,
        IReadOnlyDictionary<Guid, Media> media,
        Guid? userId)
    {
        var requestIds = requests.Select(x => x.MediaId).ToHashSet();
        var now = DateTimeOffset.UtcNow;
        foreach (var existing in allItems.Where(x => !requestIds.Contains(x.MediaId)))
        {
            existing.IsDeleted = true;
            existing.UpdatedAt = now;
            existing.UpdatedBy = userId;
        }
        for (var index = 0; index < requests.Count; index++)
        {
            var request = requests[index];
            var item = allItems.FirstOrDefault(x => x.MediaId == request.MediaId);
            var isNew = item is null;
            if (item is null)
            {
                item = new PlaylistItem { PlaylistId = playlist.Id, MediaId = request.MediaId, CreatedBy = userId };
                playlist.Items.Add(item);
            }
            item.IsDeleted = false;
            item.DisplayOrder = index + 1;
            item.DurationSeconds = request.DurationSeconds;
            item.UpdatedAt = isNew ? null : now;
            item.UpdatedBy = userId;
        }
        playlist.TotalDurationSeconds = CalculateTotalDuration(requests, media);
    }

    private async Task<int> CalculateExistingTotalDurationAsync(IReadOnlyCollection<PlaylistItem> items, CancellationToken cancellationToken)
    {
        var ids = items.Select(x => x.MediaId).Distinct().ToArray();
        var media = await _db.Media.AsNoTracking().Where(x => ids.Contains(x.Id) && !x.IsDeleted).ToDictionaryAsync(x => x.Id, cancellationToken);
        long total = 0;
        foreach (var item in items)
            total += item.DurationSeconds > 0 ? item.DurationSeconds : media.GetValueOrDefault(item.MediaId)?.DurationSeconds ?? 0;
        if (total > int.MaxValue)
            throw DeviceModuleException.BadRequest("Tổng thời lượng Playlist vượt giới hạn hỗ trợ.");
        return (int)total;
    }

    private static int CalculateTotalDuration(IReadOnlyList<PlaylistItemUpsertRequest> items, IReadOnlyDictionary<Guid, Media> media)
    {
        long total = 0;
        foreach (var item in items)
            total += item.DurationSeconds > 0 ? item.DurationSeconds : media[item.MediaId].DurationSeconds;
        if (total > int.MaxValue)
            throw DeviceModuleException.BadRequest("Tổng thời lượng Playlist vượt giới hạn hỗ trợ.");
        return (int)total;
    }

    private static void ValidatePlaylistFields(string? name, string? description, string? playMode, IReadOnlyList<PlaylistItemUpsertRequest>? items)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 255)
            throw DeviceModuleException.BadRequest("Tên Playlist bắt buộc và không vượt quá 255 ký tự.");
        if (description?.Length > 1000)
            throw DeviceModuleException.BadRequest("Mô tả Playlist không vượt quá 1000 ký tự.");
        NormalizePlayMode(playMode);
        ValidateItems(items);
    }

    private static void ValidateItems(IReadOnlyList<PlaylistItemUpsertRequest>? items)
    {
        if (items is null)
            throw DeviceModuleException.BadRequest("Danh sách Playlist item không được để trống giá trị null.");
        if (items.Any(x => x.MediaId == Guid.Empty || x.DurationSeconds < 0))
            throw DeviceModuleException.BadRequest("Playlist item có MediaId hoặc thời lượng không hợp lệ.");
        if (items.Select(x => x.MediaId).Distinct().Count() != items.Count)
            throw DeviceModuleException.Conflict("Một Media không được xuất hiện nhiều lần trong Playlist.");
    }

    private static string NormalizePlayMode(string? playMode)
    {
        var normalized = PlayModes.FirstOrDefault(x => x.Equals(playMode?.Trim(), StringComparison.OrdinalIgnoreCase));
        return normalized ?? throw DeviceModuleException.BadRequest("Chế độ phát Playlist không hợp lệ.");
    }

    private static Guid ResolveOrganizationId(UserScopeContext userScope, Guid? requestedOrganizationId)
    {
        userScope.EnsureCanAccess();
        if (userScope.IsSystemAdministrator)
            return requestedOrganizationId is { } adminOrganizationId && adminOrganizationId != Guid.Empty
                ? adminOrganizationId
                : throw DeviceModuleException.BadRequest("OrganizationId là bắt buộc với quản trị viên hệ thống.");
        if (requestedOrganizationId.HasValue && requestedOrganizationId.Value != userScope.OrganizationId)
            throw DeviceModuleException.Forbidden("Không được phép tạo dữ liệu cho tổ chức khác.");
        return userScope.OrganizationId!.Value;
    }
}

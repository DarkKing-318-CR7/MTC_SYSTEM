using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MTC_System.Common;
using MTC_System.Data;
using MTC_System.DTOs;
using MTC_System.Models;

namespace MTC_System.Services
{
    public class BroadcastService : IBroadcastService
    {
        private const string KEY_BROADCAST_MODE = "BROADCAST_MODE";
        private readonly MyDbContext _context;

        public BroadcastService(MyDbContext context)
        {
            _context = context;
        }

        // =========================================================
        // USR-BRD-01: XEM DANH SÁCH BẢN TIN (Tính toán phân quyền & Cha-Con)
        // =========================================================
        public async Task<ApiResponse<PagedResult<BroadcastListResponseDto>>> GetListAsync(
            Guid? facilityId,
            Guid? organizationId,
            string? status,
            string? search,
            int pageIndex = 1,
            int pageSize = 10,
            Guid? requestingFacilityId = null)
        {
            var query = _context.Broadcasts
                .AsNoTracking()
                .Where(b => !b.IsDeleted);

            if (facilityId.HasValue && facilityId.Value != Guid.Empty)
            {
                query = query.Where(b => b.FacilityId == facilityId.Value);
            }

            if (organizationId.HasValue && organizationId.Value != Guid.Empty)
            {
                query = query.Where(b => b.OrganizationId == organizationId.Value);
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(b => b.Status == status.Trim());
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                query = query.Where(b => b.BroadcastCode.ToLower().Contains(term) || b.BroadcastName.ToLower().Contains(term));
            }

            var totalCount = await query.CountAsync();

            var broadcasts = await query
                .OrderByDescending(b => b.CreatedAt)
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .Select(b => new
                {
                    b.Id,
                    b.BroadcastCode,
                    b.BroadcastName,
                    b.Description,
                    b.BroadcastType,
                    b.Priority,
                    b.Status,
                    b.ContentType,
                    b.OrganizationId,
                    b.FacilityId,
                    b.ApprovedAt,
                    b.ApprovedBy,
                    b.CreatedAt,
                    TotalSchedules = b.Schedules.Count(s => !s.IsDeleted)
                })
                .ToListAsync();

            var facilityIds = broadcasts.Select(i => i.FacilityId).Distinct().ToList();
            var facilityMap = await _context.Facilities
                .AsNoTracking()
                .Where(f => facilityIds.Contains(f.Id))
                .ToDictionaryAsync(f => f.Id, f => (f.FacilityName, f.ParentId));

            var ancestorFacilityIds = new HashSet<Guid>();
            if (requestingFacilityId.HasValue && requestingFacilityId.Value != Guid.Empty)
            {
                var currentF = await _context.Facilities.AsNoTracking().FirstOrDefaultAsync(f => f.Id == requestingFacilityId.Value);
                var curParentId = currentF?.ParentId;
                while (curParentId.HasValue)
                {
                    ancestorFacilityIds.Add(curParentId.Value);
                    var parentF = await _context.Facilities.AsNoTracking().FirstOrDefaultAsync(f => f.Id == curParentId.Value);
                    curParentId = parentF?.ParentId;
                }
            }

            var items = broadcasts.Select(b =>
            {
                bool isFromParent = requestingFacilityId.HasValue &&
                                    requestingFacilityId.Value != Guid.Empty &&
                                    ancestorFacilityIds.Contains(b.FacilityId);

                bool isCurrentFacility = !requestingFacilityId.HasValue || requestingFacilityId.Value == b.FacilityId;
                bool isPriority = b.Priority == 1 || b.BroadcastType == "Khẩn cấp";

                bool canEdit = isCurrentFacility && !isFromParent && b.Status == "Chờ duyệt";
                bool canDelete = isCurrentFacility && !isFromParent && (b.Status == "Chờ duyệt" || b.Status == "Từ chối duyệt" || b.Status == "Đã hủy");
                bool canApprove = b.Status == "Chờ duyệt";
                bool canReject = b.Status == "Chờ duyệt";
                bool canCancel = isCurrentFacility && !isFromParent && (b.Status == "Chưa phát" || b.Status == "Sắp tới lịch" || b.Status == "Đang phát" || b.Status == "Chờ phát" || b.Status == "Tạm ngưng");
                bool canPause = isCurrentFacility && !isFromParent && b.Status == "Đang phát" && !isPriority;
                bool canResume = isCurrentFacility && !isFromParent && b.Status == "Tạm ngưng" && !isPriority;

                facilityMap.TryGetValue(b.FacilityId, out var fInfo);

                return new BroadcastListResponseDto
                {
                    Id = b.Id,
                    BroadcastCode = b.BroadcastCode,
                    BroadcastName = b.BroadcastName,
                    Description = b.Description,
                    BroadcastType = b.BroadcastType,
                    Priority = b.Priority,
                    Status = b.Status,
                    ContentType = b.ContentType,
                    OrganizationId = b.OrganizationId,
                    FacilityId = b.FacilityId,
                    FacilityName = fInfo.FacilityName,
                    ApprovedAt = b.ApprovedAt,
                    ApprovedBy = b.ApprovedBy,
                    CreatedAt = b.CreatedAt,
                    TotalSchedules = b.TotalSchedules,
                    CanEdit = canEdit,
                    CanDelete = canDelete,
                    CanApprove = canApprove,
                    CanReject = canReject,
                    CanCancel = canCancel,
                    CanPause = canPause,
                    CanResume = canResume,
                    IsFromParent = isFromParent
                };
            }).ToList();

            var pagedResult = PagedResult<BroadcastListResponseDto>.Create(items, totalCount, pageIndex, pageSize);
            return ApiResponse<PagedResult<BroadcastListResponseDto>>.Ok(pagedResult);
        }

        // =========================================================
        // USR-BRD-03: XEM THÔNG TIN BẢN TIN CHI TIẾT
        // =========================================================
        public async Task<ApiResponse<BroadcastDetailResponseDto>> GetByIdAsync(Guid id, Guid? requestingFacilityId = null)
        {
            var broadcast = await _context.Broadcasts
                .AsNoTracking()
                .Include(b => b.Schedules.Where(s => !s.IsDeleted))
                    .ThenInclude(s => s.TimeSlots.Where(t => !t.IsDeleted))
                .Include(b => b.Schedules.Where(s => !s.IsDeleted))
                    .ThenInclude(s => s.BroadcastDevices.Where(bd => !bd.IsDeleted))
                .FirstOrDefaultAsync(b => b.Id == id && !b.IsDeleted);

            if (broadcast == null)
            {
                return ApiResponse<BroadcastDetailResponseDto>.Fail("Không tìm thấy bản tin!");
            }

            var facility = await _context.Facilities.AsNoTracking().FirstOrDefaultAsync(f => f.Id == broadcast.FacilityId);
            string? mediaName = null;
            if (broadcast.MediaId.HasValue)
            {
                var media = await _context.Media.AsNoTracking().FirstOrDefaultAsync(m => m.Id == broadcast.MediaId.Value);
                mediaName = media?.MediaName;
            }

            string? playlistName = null;
            if (broadcast.PlaylistId.HasValue)
            {
                var pl = await _context.Playlists.AsNoTracking().FirstOrDefaultAsync(p => p.Id == broadcast.PlaylistId.Value);
                playlistName = pl?.PlaylistName;
            }

            var allDeviceIds = broadcast.Schedules
                .SelectMany(s => s.BroadcastDevices)
                .Select(bd => bd.DeviceId)
                .Distinct()
                .ToList();

            var devicesMap = await _context.Devices
                .AsNoTracking()
                .Where(d => allDeviceIds.Contains(d.Id))
                .ToDictionaryAsync(d => d.Id, d => (d.DeviceName, d.DeviceCode));

            bool isFromParent = false;
            if (requestingFacilityId.HasValue && requestingFacilityId.Value != Guid.Empty && requestingFacilityId.Value != broadcast.FacilityId)
            {
                var cur = await _context.Facilities.AsNoTracking().FirstOrDefaultAsync(f => f.Id == requestingFacilityId.Value);
                var curParentId = cur?.ParentId;
                while (curParentId.HasValue)
                {
                    if (curParentId.Value == broadcast.FacilityId)
                    {
                        isFromParent = true;
                        break;
                    }
                    var parentF = await _context.Facilities.AsNoTracking().FirstOrDefaultAsync(f => f.Id == curParentId.Value);
                    curParentId = parentF?.ParentId;
                }
            }

            bool isCurrentFacility = !requestingFacilityId.HasValue || requestingFacilityId.Value == broadcast.FacilityId;
            bool isPriority = broadcast.Priority == 1 || broadcast.BroadcastType == "Khẩn cấp";

            var dto = new BroadcastDetailResponseDto
            {
                Id = broadcast.Id,
                BroadcastCode = broadcast.BroadcastCode,
                BroadcastName = broadcast.BroadcastName,
                Description = broadcast.Description,
                BroadcastType = broadcast.BroadcastType,
                Priority = broadcast.Priority,
                Status = broadcast.Status,
                RejectionReason = broadcast.RejectionReason,
                ContentType = broadcast.ContentType,
                MediaId = broadcast.MediaId,
                MediaName = mediaName,
                PlaylistId = broadcast.PlaylistId,
                PlaylistName = playlistName,
                OrganizationId = broadcast.OrganizationId,
                FacilityId = broadcast.FacilityId,
                FacilityName = facility?.FacilityName,
                ApprovedAt = broadcast.ApprovedAt,
                ApprovedBy = broadcast.ApprovedBy,
                CreatedAt = broadcast.CreatedAt,
                UpdatedAt = broadcast.UpdatedAt,
                CanEdit = isCurrentFacility && !isFromParent && broadcast.Status == "Chờ duyệt",
                CanDelete = isCurrentFacility && !isFromParent && (broadcast.Status == "Chờ duyệt" || broadcast.Status == "Từ chối duyệt" || broadcast.Status == "Đã hủy"),
                CanApprove = broadcast.Status == "Chờ duyệt",
                CanReject = broadcast.Status == "Chờ duyệt",
                CanCancel = isCurrentFacility && !isFromParent && (broadcast.Status == "Chưa phát" || broadcast.Status == "Sắp tới lịch" || broadcast.Status == "Đang phát" || broadcast.Status == "Chờ phát" || broadcast.Status == "Tạm ngưng"),
                CanPause = isCurrentFacility && !isFromParent && broadcast.Status == "Đang phát" && !isPriority,
                CanResume = isCurrentFacility && !isFromParent && broadcast.Status == "Tạm ngưng" && !isPriority,
                IsFromParent = isFromParent,
                Schedules = broadcast.Schedules.Select(s => new BroadcastScheduleResponseDto
                {
                    Id = s.Id,
                    BroadcastId = s.BroadcastId,
                    StartDate = s.StartDate,
                    EndDate = s.EndDate,
                    RepeatType = s.RepeatType,
                    RepeatDays = s.RepeatDays,
                    IsLocked = s.IsLocked,
                    TimeSlots = s.TimeSlots.Select(t => new BroadcastTimeSlotResponseDto
                    {
                        Id = t.Id,
                        BroadcastScheduleId = t.BroadcastScheduleId,
                        StartTime = t.StartTime,
                        EndTime = t.EndTime,
                        DurationSeconds = t.DurationSeconds
                    }).ToList(),
                    Devices = s.BroadcastDevices.Select(bd => new BroadcastDeviceResponseDto
                    {
                        Id = bd.Id,
                        BroadcastScheduleId = bd.BroadcastScheduleId,
                        DeviceId = bd.DeviceId,
                        DeviceName = devicesMap.TryGetValue(bd.DeviceId, out var dInfo) ? dInfo.DeviceName : null,
                        DeviceCode = dInfo.DeviceCode,
                        DeviceStatus = bd.DeviceStatus,
                        IsPreempted = bd.IsPreempted,
                        PreemptedByBroadcastId = bd.PreemptedByBroadcastId
                    }).ToList()
                }).ToList()
            };

            return ApiResponse<BroadcastDetailResponseDto>.Ok(dto);
        }

        // =========================================================
        // USR-BRD-02: THÊM BẢN TIN (Quét trùng lịch theo Chế độ Cơ sở / Thiết bị)
        // =========================================================
        public async Task<ApiResponse<BroadcastDetailResponseDto>> CreateAsync(CreateBroadcastDto dto)
        {
            var facility = await _context.Facilities.FirstOrDefaultAsync(f => f.Id == dto.FacilityId && !f.IsDeleted);
            if (facility == null)
            {
                return ApiResponse<BroadcastDetailResponseDto>.Fail("Cơ sở trực thuộc không tồn tại hoặc đã bị xóa!");
            }

            var org = await _context.Organizations.FirstOrDefaultAsync(o => o.Id == dto.OrganizationId && !o.IsDeleted);
            if (org == null)
            {
                return ApiResponse<BroadcastDetailResponseDto>.Fail("Tổ chức không tồn tại hoặc đã bị xóa!");
            }

            // Đọc chế độ phát từ SystemSettings: "Phát theo cơ sở" hoặc "Phát theo thiết bị"
            var broadcastMode = await GetBroadcastModeAsync(dto.OrganizationId);

            int playlistDuration = 0;
            if (dto.ContentType == "Playlist")
            {
                if (!dto.PlaylistId.HasValue)
                    return ApiResponse<BroadcastDetailResponseDto>.Fail("Bản tin loại Playlist bắt buộc phải chọn PlaylistId!");
                var playlist = await _context.Playlists.FirstOrDefaultAsync(p => p.Id == dto.PlaylistId.Value && !p.IsDeleted);
                if (playlist == null)
                    return ApiResponse<BroadcastDetailResponseDto>.Fail("Playlist được chọn không tồn tại!");
                playlistDuration = playlist.TotalDurationSeconds;
            }
            else if (dto.ContentType == "File")
            {
                if (!dto.MediaId.HasValue)
                    return ApiResponse<BroadcastDetailResponseDto>.Fail("Bản tin loại File bắt buộc phải chọn MediaId!");
                var mediaExists = await _context.Media.AnyAsync(m => m.Id == dto.MediaId.Value && !m.IsDeleted);
                if (!mediaExists)
                    return ApiResponse<BroadcastDetailResponseDto>.Fail("Tệp Media được chọn không tồn tại!");
            }

            bool isPriority = dto.Priority == 1 || dto.BroadcastType == "Khẩn cấp";

            string code = string.IsNullOrWhiteSpace(dto.BroadcastCode)
                ? $"BC-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..6].ToUpper()}"
                : dto.BroadcastCode.Trim();

            var codeExists = await _context.Broadcasts.AnyAsync(b => b.BroadcastCode == code && !b.IsDeleted);
            if (codeExists)
            {
                return ApiResponse<BroadcastDetailResponseDto>.Fail($"Mã bản tin '{code}' đã tồn tại trong hệ thống!");
            }

            // Lấy toàn bộ thiết bị của cơ sở nếu là chế độ "Phát theo cơ sở"
            List<Guid> facilityAllDeviceIds = new List<Guid>();
            if (broadcastMode == "Phát theo cơ sở")
            {
                facilityAllDeviceIds = await _context.Devices
                    .AsNoTracking()
                    .Where(d => d.FacilityId == dto.FacilityId && !d.IsDeleted)
                    .Select(d => d.Id)
                    .ToListAsync();
            }

            // =========================================================
            // KIỂM TRA TRÙNG LỊCH (CONFLICT DETECTION)
            // =========================================================
            if (dto.Schedules != null && dto.Schedules.Any())
            {
                foreach (var sch in dto.Schedules)
                {
                    if (!isPriority && sch.EndDate.Date < sch.StartDate.Date)
                    {
                        return ApiResponse<BroadcastDetailResponseDto>.Fail("Ngày kết thúc phát không thể nhỏ hơn ngày bắt đầu!");
                    }

                    // Nếu ở chế độ Phát theo cơ sở: tự gán toàn bộ thiết bị của cơ sở nếu chưa chỉ định
                    var targetDeviceIds = (broadcastMode == "Phát theo cơ sở")
                        ? facilityAllDeviceIds
                        : (sch.DeviceIds ?? new List<Guid>());

                    if (sch.TimeSlots != null && sch.TimeSlots.Any() && targetDeviceIds.Any())
                    {
                        var conflictResult = await CheckConflictEngineAsync(
                            dto.OrganizationId,
                            dto.FacilityId,
                            targetDeviceIds,
                            sch.StartDate,
                            sch.EndDate,
                            sch.TimeSlots,
                            null,
                            null);

                        if (conflictResult.HasConflict)
                        {
                            if (!dto.Overwrite && !isPriority)
                            {
                                var errorList = conflictResult.ConflictingBroadcasts
                                    .Select(c => $"Bản tin '{c.BroadcastName}' ({c.BroadcastCode}) - { (broadcastMode == "Phát theo cơ sở" ? $"Cơ sở: {c.FacilityName}" : $"Thiết bị: {c.DeviceName}") } [{c.StartTime:hh\\:mm} - {c.EndTime:hh\\:mm}]")
                                    .Distinct()
                                    .ToList();

                                return ApiResponse<BroadcastDetailResponseDto>.Fail(conflictResult.SummaryMessage, errorList);
                            }
                        }
                    }
                }
            }

            var broadcast = new Broadcast
            {
                BroadcastCode = code,
                BroadcastName = dto.BroadcastName.Trim(),
                Description = dto.Description?.Trim(),
                BroadcastType = string.IsNullOrWhiteSpace(dto.BroadcastType) ? "Thường" : dto.BroadcastType.Trim(),
                Priority = isPriority ? 1 : 2,
                Status = "Chờ duyệt",
                ContentType = dto.ContentType.Trim(),
                MediaId = dto.ContentType == "File" ? dto.MediaId : null,
                PlaylistId = dto.ContentType == "Playlist" ? dto.PlaylistId : null,
                OrganizationId = dto.OrganizationId,
                FacilityId = dto.FacilityId,
                CreatedAt = DateTimeOffset.UtcNow
            };

            if (dto.Schedules != null && dto.Schedules.Any())
            {
                foreach (var schDto in dto.Schedules)
                {
                    var schedule = new BroadcastSchedule
                    {
                        BroadcastId = broadcast.Id,
                        StartDate = schDto.StartDate,
                        EndDate = schDto.EndDate,
                        RepeatType = string.IsNullOrWhiteSpace(schDto.RepeatType) ? "Không lặp" : schDto.RepeatType.Trim(),
                        RepeatDays = schDto.RepeatDays,
                        CreatedAt = DateTimeOffset.UtcNow
                    };

                    if (schDto.TimeSlots != null)
                    {
                        foreach (var slotDto in schDto.TimeSlots)
                        {
                            int duration = (dto.ContentType == "Playlist" && playlistDuration > 0)
                                ? playlistDuration
                                : (slotDto.DurationSeconds > 0 ? slotDto.DurationSeconds : (int)(slotDto.EndTime - slotDto.StartTime).TotalSeconds);

                            schedule.TimeSlots.Add(new BroadcastTimeSlot
                            {
                                BroadcastScheduleId = schedule.Id,
                                StartTime = slotDto.StartTime,
                                EndTime = slotDto.EndTime,
                                DurationSeconds = duration,
                                CreatedAt = DateTimeOffset.UtcNow
                            });
                        }
                    }

                    // Gán thiết bị: nếu chế độ cơ sở thì gán toàn bộ thiết bị cơ sở
                    var assignedDeviceIds = (broadcastMode == "Phát theo cơ sở")
                        ? facilityAllDeviceIds
                        : (schDto.DeviceIds?.Distinct().ToList() ?? new List<Guid>());

                    foreach (var devId in assignedDeviceIds)
                    {
                        schedule.BroadcastDevices.Add(new BroadcastDevice
                        {
                            BroadcastScheduleId = schedule.Id,
                            DeviceId = devId,
                            DeviceStatus = "Chờ phát",
                            CreatedAt = DateTimeOffset.UtcNow
                        });
                    }

                    broadcast.Schedules.Add(schedule);
                }
            }

            _context.Broadcasts.Add(broadcast);
            await _context.SaveChangesAsync();

            // Ghi đè các bản tin cũ nếu Overwrite hoặc là bản tin ưu tiên
            if (dto.Overwrite || isPriority)
            {
                await PreemptConflictingDevicesAsync(broadcast.Id);
            }

            return await GetByIdAsync(broadcast.Id);
        }

        // =========================================================
        // API CHECK CONFLICT TRƯỚC KHI TẠO (CONFLICT ENGINE API)
        // =========================================================
        public async Task<ApiResponse<ConflictCheckResultDto>> CheckConflictApiAsync(CheckConflictRequestDto dto)
        {
            var broadcastMode = await GetBroadcastModeAsync(dto.OrganizationId);
            List<Guid> targetDeviceIds;

            if (broadcastMode == "Phát theo cơ sở")
            {
                targetDeviceIds = await _context.Devices
                    .AsNoTracking()
                    .Where(d => d.FacilityId == dto.FacilityId && !d.IsDeleted)
                    .Select(d => d.Id)
                    .ToListAsync();
            }
            else
            {
                targetDeviceIds = dto.DeviceIds ?? new List<Guid>();
            }

            var result = await CheckConflictEngineAsync(
                dto.OrganizationId,
                dto.FacilityId,
                targetDeviceIds,
                dto.StartDate,
                dto.EndDate,
                dto.TimeSlots,
                dto.ExcludeBroadcastId,
                null);

            return ApiResponse<ConflictCheckResultDto>.Ok(result);
        }

        // =========================================================
        // USR-BRD-04: CHỈNH SỬA (Chỉ sửa bản tin Chờ duyệt)
        // =========================================================
        public async Task<ApiResponse<BroadcastDetailResponseDto>> UpdateAsync(Guid id, UpdateBroadcastDto dto, Guid? requestingFacilityId = null)
        {
            var broadcast = await _context.Broadcasts.FirstOrDefaultAsync(b => b.Id == id && !b.IsDeleted);
            if (broadcast == null)
            {
                return ApiResponse<BroadcastDetailResponseDto>.Fail("Không tìm thấy bản tin!");
            }

            if (broadcast.Status != "Chờ duyệt")
            {
                return ApiResponse<BroadcastDetailResponseDto>.Fail($"Không thể chỉnh sửa bản tin khi đang ở trạng thái '{broadcast.Status}'. Chỉ sửa bản tin 'Chờ duyệt'!");
            }

            if (requestingFacilityId.HasValue && requestingFacilityId.Value != Guid.Empty && requestingFacilityId.Value != broadcast.FacilityId)
            {
                return ApiResponse<BroadcastDetailResponseDto>.Fail("Bản tin do cơ sở cha tạo chỉ cho phép xem chi tiết, không được chỉnh sửa!");
            }

            if (dto.ContentType == "File")
            {
                if (!dto.MediaId.HasValue)
                    return ApiResponse<BroadcastDetailResponseDto>.Fail("Bản tin loại File bắt buộc phải chọn MediaId!");
                var mediaExists = await _context.Media.AnyAsync(m => m.Id == dto.MediaId.Value && !m.IsDeleted);
                if (!mediaExists)
                    return ApiResponse<BroadcastDetailResponseDto>.Fail("Tệp Media được chọn không tồn tại!");
            }
            else if (dto.ContentType == "Playlist")
            {
                if (!dto.PlaylistId.HasValue)
                    return ApiResponse<BroadcastDetailResponseDto>.Fail("Bản tin loại Playlist bắt buộc phải chọn PlaylistId!");
                var playlistExists = await _context.Playlists.AnyAsync(p => p.Id == dto.PlaylistId.Value && !p.IsDeleted);
                if (!playlistExists)
                    return ApiResponse<BroadcastDetailResponseDto>.Fail("Playlist được chọn không tồn tại!");
            }

            broadcast.BroadcastName = dto.BroadcastName.Trim();
            broadcast.Description = dto.Description?.Trim();
            broadcast.BroadcastType = dto.BroadcastType.Trim();
            broadcast.Priority = (dto.Priority == 1 || dto.BroadcastType == "Khẩn cấp") ? 1 : 2;
            broadcast.ContentType = dto.ContentType.Trim();
            broadcast.MediaId = dto.ContentType == "File" ? dto.MediaId : null;
            broadcast.PlaylistId = dto.ContentType == "Playlist" ? dto.PlaylistId : null;
            broadcast.UpdatedAt = DateTimeOffset.UtcNow;

            await _context.SaveChangesAsync();
            return await GetByIdAsync(broadcast.Id);
        }

        // =========================================================
        // USR-BRD-05: XÓA (Chỉ xóa bản tin Chờ duyệt, Từ chối duyệt hoặc Đã hủy)
        // =========================================================
        public async Task<ApiResponse<bool>> DeleteAsync(Guid id, Guid? requestingFacilityId = null)
        {
            var broadcast = await _context.Broadcasts
                .Include(b => b.Schedules)
                    .ThenInclude(s => s.TimeSlots)
                .Include(b => b.Schedules)
                    .ThenInclude(s => s.BroadcastDevices)
                .FirstOrDefaultAsync(b => b.Id == id && !b.IsDeleted);

            if (broadcast == null)
            {
                return ApiResponse<bool>.Fail("Không tìm thấy bản tin!");
            }

            if (requestingFacilityId.HasValue && requestingFacilityId.Value != Guid.Empty && requestingFacilityId.Value != broadcast.FacilityId)
            {
                return ApiResponse<bool>.Fail("Không có quyền xóa bản tin của cơ sở cấp trên!");
            }

            var allowedStatuses = new[] { "Chờ duyệt", "Từ chối duyệt", "Đã hủy" };
            if (!allowedStatuses.Contains(broadcast.Status))
            {
                return ApiResponse<bool>.Fail($"Không thể xóa bản tin đang ở trạng thái '{broadcast.Status}'. Chỉ xóa bản tin Chờ duyệt, Từ chối duyệt hoặc Đã hủy!");
            }

            broadcast.IsDeleted = true;
            broadcast.UpdatedAt = DateTimeOffset.UtcNow;

            foreach (var s in broadcast.Schedules)
            {
                s.IsDeleted = true;
                s.UpdatedAt = DateTimeOffset.UtcNow;
                foreach (var ts in s.TimeSlots)
                {
                    ts.IsDeleted = true;
                    ts.UpdatedAt = DateTimeOffset.UtcNow;
                }
                foreach (var bd in s.BroadcastDevices)
                {
                    bd.IsDeleted = true;
                    bd.UpdatedAt = DateTimeOffset.UtcNow;
                }
            }

            await _context.SaveChangesAsync();
            return ApiResponse<bool>.Ok(true, "Xóa bản tin thành công!");
        }

        // =========================================================
        // USR-BRD-06: DUYỆT (Chỉ duyệt bản tin Chờ duyệt; tin ưu tiên đè tin trước)
        // =========================================================
        public async Task<ApiResponse<BroadcastDetailResponseDto>> ApproveAsync(Guid id, ApproveBroadcastDto dto)
        {
            var broadcast = await _context.Broadcasts
                .Include(b => b.Schedules)
                    .ThenInclude(s => s.BroadcastDevices)
                .FirstOrDefaultAsync(b => b.Id == id && !b.IsDeleted);

            if (broadcast == null)
            {
                return ApiResponse<BroadcastDetailResponseDto>.Fail("Không tìm thấy bản tin!");
            }

            if (broadcast.Status != "Chờ duyệt")
            {
                return ApiResponse<BroadcastDetailResponseDto>.Fail($"Bản tin đang ở trạng thái '{broadcast.Status}'. Chỉ duyệt bản tin Chờ duyệt!");
            }

            broadcast.Status = "Chưa phát";
            broadcast.ApprovedAt = DateTimeOffset.UtcNow;
            broadcast.ApprovedBy = dto.ApprovedBy;
            broadcast.RejectionReason = null;
            broadcast.UpdatedAt = DateTimeOffset.UtcNow;

            bool isPriority = broadcast.Priority == 1 || broadcast.BroadcastType == "Khẩn cấp";
            if (isPriority || dto.Overwrite)
            {
                await PreemptConflictingDevicesAsync(broadcast.Id);
            }

            await _context.SaveChangesAsync();
            return await GetByIdAsync(broadcast.Id);
        }

        // =========================================================
        // USR-BRD-07: TỪ CHỐI PHÊ DUYỆT (Chỉ với bản tin Chờ duyệt. Lý do tối đa 510 ký tự)
        // =========================================================
        public async Task<ApiResponse<BroadcastDetailResponseDto>> RejectAsync(Guid id, RejectBroadcastDto dto)
        {
            var broadcast = await _context.Broadcasts.FirstOrDefaultAsync(b => b.Id == id && !b.IsDeleted);
            if (broadcast == null)
            {
                return ApiResponse<BroadcastDetailResponseDto>.Fail("Không tìm thấy bản tin!");
            }

            if (broadcast.Status != "Chờ duyệt")
            {
                return ApiResponse<BroadcastDetailResponseDto>.Fail($"Bản tin đang ở trạng thái '{broadcast.Status}'. Chỉ từ chối bản tin Chờ duyệt!");
            }

            if (string.IsNullOrWhiteSpace(dto.RejectionReason))
            {
                return ApiResponse<BroadcastDetailResponseDto>.Fail("Bắt buộc phải nhập lý do từ chối!");
            }
            if (dto.RejectionReason.Trim().Length > 510)
            {
                return ApiResponse<BroadcastDetailResponseDto>.Fail("Lý do từ chối tối đa 510 ký tự!");
            }

            broadcast.Status = "Từ chối duyệt";
            broadcast.RejectionReason = dto.RejectionReason.Trim();
            broadcast.ApprovedAt = DateTimeOffset.UtcNow;
            broadcast.ApprovedBy = dto.RejectedBy;
            broadcast.UpdatedAt = DateTimeOffset.UtcNow;

            await _context.SaveChangesAsync();
            return await GetByIdAsync(broadcast.Id);
        }

        // =========================================================
        // USR-BRD-08: HỦY BẢN TIN (Chỉ hủy bản tin Đã duyệt)
        // =========================================================
        public async Task<ApiResponse<bool>> CancelAsync(Guid id, CancelBroadcastDto dto, Guid? requestingFacilityId = null)
        {
            var broadcast = await _context.Broadcasts.FirstOrDefaultAsync(b => b.Id == id && !b.IsDeleted);
            if (broadcast == null)
            {
                return ApiResponse<bool>.Fail("Không tìm thấy bản tin!");
            }

            if (requestingFacilityId.HasValue && requestingFacilityId.Value != Guid.Empty && requestingFacilityId.Value != broadcast.FacilityId)
            {
                return ApiResponse<bool>.Fail("Không có quyền hủy bản tin của cơ sở cấp trên!");
            }

            var approvedStatuses = new[] { "Chưa phát", "Sắp tới lịch", "Đang phát", "Chờ phát", "Tạm ngưng" };
            if (!approvedStatuses.Contains(broadcast.Status))
            {
                return ApiResponse<bool>.Fail($"Không thể hủy bản tin đang ở trạng thái '{broadcast.Status}'. Chỉ hủy bản tin Đã duyệt!");
            }

            broadcast.Status = "Đã hủy";
            if (!string.IsNullOrWhiteSpace(dto.Reason))
            {
                broadcast.Description = (broadcast.Description + $" [Lý do hủy: {dto.Reason.Trim()}]").Trim();
            }
            broadcast.UpdatedAt = DateTimeOffset.UtcNow;

            await _context.SaveChangesAsync();
            return ApiResponse<bool>.Ok(true, "Đã hủy bản tin thành công!");
        }

        // =========================================================
        // USR-BRD-09: DỪNG PHÁT (Chỉ với bản tin Đang phát; không áp dụng cho bản tin ưu tiên)
        // =========================================================
        public async Task<ApiResponse<BroadcastDetailResponseDto>> PauseAsync(Guid id, PauseBroadcastDto dto)
        {
            var broadcast = await _context.Broadcasts
                .Include(b => b.Schedules)
                    .ThenInclude(s => s.BroadcastDevices)
                .FirstOrDefaultAsync(b => b.Id == id && !b.IsDeleted);

            if (broadcast == null)
            {
                return ApiResponse<BroadcastDetailResponseDto>.Fail("Không tìm thấy bản tin!");
            }

            if (broadcast.Priority == 1 || broadcast.BroadcastType == "Khẩn cấp")
            {
                return ApiResponse<BroadcastDetailResponseDto>.Fail("Không áp dụng tạm dừng phát cho bản tin ưu tiên!");
            }

            if (broadcast.Status != "Đang phát")
            {
                return ApiResponse<BroadcastDetailResponseDto>.Fail($"Bản tin đang ở trạng thái '{broadcast.Status}'. Chỉ dừng phát bản tin Đang phát!");
            }

            broadcast.Status = "Tạm ngưng";
            broadcast.UpdatedAt = DateTimeOffset.UtcNow;

            foreach (var sch in broadcast.Schedules)
            {
                foreach (var bd in sch.BroadcastDevices)
                {
                    if (bd.DeviceStatus == "Đang phát")
                    {
                        bd.DeviceStatus = "Tạm ngưng";
                        bd.UpdatedAt = DateTimeOffset.UtcNow;
                    }
                }
            }

            await _context.SaveChangesAsync();
            return await GetByIdAsync(broadcast.Id);
        }

        // =========================================================
        // USR-BRD-10: TIẾP TỤC PHÁT (Chỉ với bản tin Tạm ngưng)
        // =========================================================
        public async Task<ApiResponse<BroadcastDetailResponseDto>> ResumeAsync(Guid id)
        {
            var broadcast = await _context.Broadcasts
                .Include(b => b.Schedules)
                    .ThenInclude(s => s.BroadcastDevices)
                .FirstOrDefaultAsync(b => b.Id == id && !b.IsDeleted);

            if (broadcast == null)
            {
                return ApiResponse<BroadcastDetailResponseDto>.Fail("Không tìm thấy bản tin!");
            }

            if (broadcast.Status != "Tạm ngưng")
            {
                return ApiResponse<BroadcastDetailResponseDto>.Fail($"Bản tin đang ở trạng thái '{broadcast.Status}'. Chỉ tiếp tục phát bản tin Tạm ngưng!");
            }

            broadcast.Status = "Đang phát";
            broadcast.UpdatedAt = DateTimeOffset.UtcNow;

            foreach (var sch in broadcast.Schedules)
            {
                foreach (var bd in sch.BroadcastDevices)
                {
                    if (bd.DeviceStatus == "Tạm ngưng")
                    {
                        bd.DeviceStatus = "Đang phát";
                        bd.UpdatedAt = DateTimeOffset.UtcNow;
                    }
                }
            }

            await _context.SaveChangesAsync();
            return await GetByIdAsync(broadcast.Id);
        }

        // =========================================================
        // USR-BRD-11: BẢN TIN CƠ SỞ CON (Xem danh sách và thông tin bản tin của cơ sở con)
        // =========================================================
        public async Task<ApiResponse<PagedResult<BroadcastListResponseDto>>> GetSubFacilityBroadcastsAsync(
            Guid parentFacilityId,
            string? search,
            int pageIndex = 1,
            int pageSize = 10)
        {
            var parent = await _context.Facilities.AsNoTracking().FirstOrDefaultAsync(f => f.Id == parentFacilityId && !f.IsDeleted);
            if (parent == null)
            {
                return ApiResponse<PagedResult<BroadcastListResponseDto>>.Fail("Không tìm thấy cơ sở cha!");
            }

            var subFacilityIds = await GetAllSubFacilityIdsAsync(parentFacilityId);
            if (!subFacilityIds.Any())
            {
                var emptyResult = PagedResult<BroadcastListResponseDto>.Create(new List<BroadcastListResponseDto>(), 0, pageIndex, pageSize);
                return ApiResponse<PagedResult<BroadcastListResponseDto>>.Ok(emptyResult);
            }

            var query = _context.Broadcasts
                .AsNoTracking()
                .Where(b => !b.IsDeleted && subFacilityIds.Contains(b.FacilityId));

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                query = query.Where(b => b.BroadcastCode.ToLower().Contains(term) || b.BroadcastName.ToLower().Contains(term));
            }

            var totalCount = await query.CountAsync();

            var broadcasts = await query
                .OrderByDescending(b => b.CreatedAt)
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .Select(b => new
                {
                    b.Id,
                    b.BroadcastCode,
                    b.BroadcastName,
                    b.Description,
                    b.BroadcastType,
                    b.Priority,
                    b.Status,
                    b.ContentType,
                    b.OrganizationId,
                    b.FacilityId,
                    b.ApprovedAt,
                    b.ApprovedBy,
                    b.CreatedAt,
                    TotalSchedules = b.Schedules.Count(s => !s.IsDeleted)
                })
                .ToListAsync();

            var facilityMap = await _context.Facilities
                .AsNoTracking()
                .Where(f => subFacilityIds.Contains(f.Id))
                .ToDictionaryAsync(f => f.Id, f => f.FacilityName);

            var items = broadcasts.Select(b =>
            {
                facilityMap.TryGetValue(b.FacilityId, out var fName);
                return new BroadcastListResponseDto
                {
                    Id = b.Id,
                    BroadcastCode = b.BroadcastCode,
                    BroadcastName = b.BroadcastName,
                    Description = b.Description,
                    BroadcastType = b.BroadcastType,
                    Priority = b.Priority,
                    Status = b.Status,
                    ContentType = b.ContentType,
                    OrganizationId = b.OrganizationId,
                    FacilityId = b.FacilityId,
                    FacilityName = fName,
                    ApprovedAt = b.ApprovedAt,
                    ApprovedBy = b.ApprovedBy,
                    CreatedAt = b.CreatedAt,
                    TotalSchedules = b.TotalSchedules,
                    CanEdit = false,
                    CanDelete = b.Status == "Chờ duyệt" || b.Status == "Từ chối duyệt" || b.Status == "Đã hủy",
                    CanApprove = b.Status == "Chờ duyệt",
                    CanReject = b.Status == "Chờ duyệt",
                    CanCancel = b.Status == "Chưa phát" || b.Status == "Sắp tới lịch" || b.Status == "Đang phát" || b.Status == "Chờ phát" || b.Status == "Tạm ngưng",
                    CanPause = b.Status == "Đang phát" && b.Priority != 1 && b.BroadcastType != "Khẩn cấp",
                    CanResume = b.Status == "Tạm ngưng" && b.Priority != 1 && b.BroadcastType != "Khẩn cấp",
                    IsFromParent = false
                };
            }).ToList();

            var pagedResult = PagedResult<BroadcastListResponseDto>.Create(items, totalCount, pageIndex, pageSize);
            return ApiResponse<PagedResult<BroadcastListResponseDto>>.Ok(pagedResult);
        }

        private async Task<List<Guid>> GetAllSubFacilityIdsAsync(Guid parentFacilityId)
        {
            var result = new List<Guid>();
            var directChildren = await _context.Facilities
                .AsNoTracking()
                .Where(f => f.ParentId == parentFacilityId && !f.IsDeleted)
                .Select(f => f.Id)
                .ToListAsync();

            result.AddRange(directChildren);
            foreach (var childId in directChildren)
            {
                var subChildren = await GetAllSubFacilityIdsAsync(childId);
                result.AddRange(subChildren);
            }
            return result;
        }

        // =========================================================
        // QUẢN LÝ LỊCH PHÁT (BROADCAST SCHEDULES)
        // =========================================================
        public async Task<ApiResponse<List<BroadcastScheduleResponseDto>>> GetSchedulesAsync(Guid broadcastId)
        {
            var schedules = await _context.BroadcastSchedules
                .AsNoTracking()
                .Where(s => s.BroadcastId == broadcastId && !s.IsDeleted)
                .Include(s => s.TimeSlots.Where(t => !t.IsDeleted))
                .Include(s => s.BroadcastDevices.Where(d => !d.IsDeleted))
                .ToListAsync();

            var allDeviceIds = schedules.SelectMany(s => s.BroadcastDevices).Select(bd => bd.DeviceId).Distinct().ToList();
            var devicesMap = await _context.Devices
                .AsNoTracking()
                .Where(d => allDeviceIds.Contains(d.Id))
                .ToDictionaryAsync(d => d.Id, d => (d.DeviceName, d.DeviceCode));

            var result = schedules.Select(s => new BroadcastScheduleResponseDto
            {
                Id = s.Id,
                BroadcastId = s.BroadcastId,
                StartDate = s.StartDate,
                EndDate = s.EndDate,
                RepeatType = s.RepeatType,
                RepeatDays = s.RepeatDays,
                IsLocked = s.IsLocked,
                TimeSlots = s.TimeSlots.Select(t => new BroadcastTimeSlotResponseDto
                {
                    Id = t.Id,
                    BroadcastScheduleId = t.BroadcastScheduleId,
                    StartTime = t.StartTime,
                    EndTime = t.EndTime,
                    DurationSeconds = t.DurationSeconds
                }).ToList(),
                Devices = s.BroadcastDevices.Select(bd => new BroadcastDeviceResponseDto
                {
                    Id = bd.Id,
                    BroadcastScheduleId = bd.BroadcastScheduleId,
                    DeviceId = bd.DeviceId,
                    DeviceName = devicesMap.TryGetValue(bd.DeviceId, out var dInfo) ? dInfo.DeviceName : null,
                    DeviceCode = dInfo.DeviceCode,
                    DeviceStatus = bd.DeviceStatus,
                    IsPreempted = bd.IsPreempted,
                    PreemptedByBroadcastId = bd.PreemptedByBroadcastId
                }).ToList()
            }).ToList();

            return ApiResponse<List<BroadcastScheduleResponseDto>>.Ok(result);
        }

        public async Task<ApiResponse<BroadcastScheduleResponseDto>> AddScheduleAsync(Guid broadcastId, CreateBroadcastScheduleDto dto)
        {
            var broadcast = await _context.Broadcasts.FirstOrDefaultAsync(b => b.Id == broadcastId && !b.IsDeleted);
            if (broadcast == null)
            {
                return ApiResponse<BroadcastScheduleResponseDto>.Fail("Không tìm thấy bản tin!");
            }

            var broadcastMode = await GetBroadcastModeAsync(broadcast.OrganizationId);
            bool isPriority = broadcast.Priority == 1 || broadcast.BroadcastType == "Khẩn cấp";

            if (!isPriority && dto.EndDate.Date < dto.StartDate.Date)
            {
                return ApiResponse<BroadcastScheduleResponseDto>.Fail("Ngày kết thúc phát không thể nhỏ hơn ngày bắt đầu!");
            }

            int playlistDuration = 0;
            if (broadcast.ContentType == "Playlist" && broadcast.PlaylistId.HasValue)
            {
                var pl = await _context.Playlists.AsNoTracking().FirstOrDefaultAsync(p => p.Id == broadcast.PlaylistId.Value);
                if (pl != null) playlistDuration = pl.TotalDurationSeconds;
            }

            List<Guid> targetDeviceIds;
            if (broadcastMode == "Phát theo cơ sở")
            {
                targetDeviceIds = await _context.Devices
                    .AsNoTracking()
                    .Where(d => d.FacilityId == broadcast.FacilityId && !d.IsDeleted)
                    .Select(d => d.Id)
                    .ToListAsync();
            }
            else
            {
                targetDeviceIds = dto.DeviceIds ?? new List<Guid>();
            }

            // Quét trùng lịch (Conflict Check)
            if (targetDeviceIds.Any() && dto.TimeSlots != null && dto.TimeSlots.Any())
            {
                var conflict = await CheckConflictEngineAsync(
                    broadcast.OrganizationId,
                    broadcast.FacilityId,
                    targetDeviceIds,
                    dto.StartDate,
                    dto.EndDate,
                    dto.TimeSlots,
                    broadcast.Id,
                    null);

                if (conflict.HasConflict && !dto.Overwrite && !isPriority)
                {
                    var errors = conflict.ConflictingBroadcasts
                        .Select(c => $"Bản tin '{c.BroadcastName}' ({c.BroadcastCode}) - { (broadcastMode == "Phát theo cơ sở" ? $"Cơ sở: {c.FacilityName}" : $"Thiết bị: {c.DeviceName}") } [{c.StartTime:hh\\:mm} - {c.EndTime:hh\\:mm}]")
                        .Distinct()
                        .ToList();

                    return ApiResponse<BroadcastScheduleResponseDto>.Fail(conflict.SummaryMessage, errors);
                }
            }

            var schedule = new BroadcastSchedule
            {
                BroadcastId = broadcastId,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                RepeatType = string.IsNullOrWhiteSpace(dto.RepeatType) ? "Không lặp" : dto.RepeatType.Trim(),
                RepeatDays = dto.RepeatDays,
                CreatedAt = DateTimeOffset.UtcNow
            };

            if (dto.TimeSlots != null)
            {
                foreach (var slotDto in dto.TimeSlots)
                {
                    int duration = (broadcast.ContentType == "Playlist" && playlistDuration > 0)
                        ? playlistDuration
                        : (slotDto.DurationSeconds > 0 ? slotDto.DurationSeconds : (int)(slotDto.EndTime - slotDto.StartTime).TotalSeconds);

                    schedule.TimeSlots.Add(new BroadcastTimeSlot
                    {
                        BroadcastScheduleId = schedule.Id,
                        StartTime = slotDto.StartTime,
                        EndTime = slotDto.EndTime,
                        DurationSeconds = duration,
                        CreatedAt = DateTimeOffset.UtcNow
                    });
                }
            }

            foreach (var devId in targetDeviceIds)
            {
                schedule.BroadcastDevices.Add(new BroadcastDevice
                {
                    BroadcastScheduleId = schedule.Id,
                    DeviceId = devId,
                    DeviceStatus = "Chờ phát",
                    CreatedAt = DateTimeOffset.UtcNow
                });
            }

            _context.BroadcastSchedules.Add(schedule);
            await _context.SaveChangesAsync();

            if (dto.Overwrite || isPriority)
            {
                await PreemptConflictingDevicesAsync(broadcast.Id);
            }

            var allSchedules = await GetSchedulesAsync(broadcastId);
            var createdSchedule = allSchedules.Data?.FirstOrDefault(s => s.Id == schedule.Id);
            return ApiResponse<BroadcastScheduleResponseDto>.Ok(createdSchedule!);
        }

        public async Task<ApiResponse<BroadcastScheduleResponseDto>> UpdateScheduleAsync(Guid scheduleId, UpdateBroadcastScheduleDto dto)
        {
            var schedule = await _context.BroadcastSchedules
                .Include(s => s.TimeSlots)
                .Include(s => s.BroadcastDevices)
                .FirstOrDefaultAsync(s => s.Id == scheduleId && !s.IsDeleted);

            if (schedule == null)
            {
                return ApiResponse<BroadcastScheduleResponseDto>.Fail("Không tìm thấy lịch phát!");
            }

            if (schedule.IsLocked)
            {
                return ApiResponse<BroadcastScheduleResponseDto>.Fail("Lịch phát đã bị khóa do bản tin đang trong trạng thái phát!");
            }

            var broadcast = await _context.Broadcasts.AsNoTracking().FirstOrDefaultAsync(b => b.Id == schedule.BroadcastId);
            if (broadcast == null)
            {
                return ApiResponse<BroadcastScheduleResponseDto>.Fail("Không tìm thấy bản tin!");
            }

            var broadcastMode = await GetBroadcastModeAsync(broadcast.OrganizationId);
            bool isPriority = broadcast.Priority == 1 || broadcast.BroadcastType == "Khẩn cấp";

            if (!isPriority && dto.EndDate.Date < dto.StartDate.Date)
            {
                return ApiResponse<BroadcastScheduleResponseDto>.Fail("Ngày kết thúc phát không thể nhỏ hơn ngày bắt đầu!");
            }

            int playlistDuration = 0;
            if (broadcast.ContentType == "Playlist" && broadcast.PlaylistId.HasValue)
            {
                var pl = await _context.Playlists.AsNoTracking().FirstOrDefaultAsync(p => p.Id == broadcast.PlaylistId.Value);
                if (pl != null) playlistDuration = pl.TotalDurationSeconds;
            }

            List<Guid> targetDeviceIds;
            if (broadcastMode == "Phát theo cơ sở")
            {
                targetDeviceIds = await _context.Devices
                    .AsNoTracking()
                    .Where(d => d.FacilityId == broadcast.FacilityId && !d.IsDeleted)
                    .Select(d => d.Id)
                    .ToListAsync();
            }
            else
            {
                targetDeviceIds = dto.DeviceIds ?? new List<Guid>();
            }

            // Quét trùng lịch (Conflict Check)
            if (targetDeviceIds.Any() && dto.TimeSlots != null && dto.TimeSlots.Any())
            {
                var conflict = await CheckConflictEngineAsync(
                    broadcast.OrganizationId,
                    broadcast.FacilityId,
                    targetDeviceIds,
                    dto.StartDate,
                    dto.EndDate,
                    dto.TimeSlots,
                    broadcast.Id,
                    scheduleId);

                if (conflict.HasConflict && !dto.Overwrite && !isPriority)
                {
                    var errors = conflict.ConflictingBroadcasts
                        .Select(c => $"Bản tin '{c.BroadcastName}' ({c.BroadcastCode}) - { (broadcastMode == "Phát theo cơ sở" ? $"Cơ sở: {c.FacilityName}" : $"Thiết bị: {c.DeviceName}") } [{c.StartTime:hh\\:mm} - {c.EndTime:hh\\:mm}]")
                        .Distinct()
                        .ToList();

                    return ApiResponse<BroadcastScheduleResponseDto>.Fail(conflict.SummaryMessage, errors);
                }
            }

            schedule.StartDate = dto.StartDate;
            schedule.EndDate = dto.EndDate;
            schedule.RepeatType = string.IsNullOrWhiteSpace(dto.RepeatType) ? "Không lặp" : dto.RepeatType.Trim();
            schedule.RepeatDays = dto.RepeatDays;
            schedule.UpdatedAt = DateTimeOffset.UtcNow;

            foreach (var slot in schedule.TimeSlots)
            {
                slot.IsDeleted = true;
                slot.UpdatedAt = DateTimeOffset.UtcNow;
            }

            if (dto.TimeSlots != null)
            {
                foreach (var slotDto in dto.TimeSlots)
                {
                    int duration = (broadcast.ContentType == "Playlist" && playlistDuration > 0)
                        ? playlistDuration
                        : (slotDto.DurationSeconds > 0 ? slotDto.DurationSeconds : (int)(slotDto.EndTime - slotDto.StartTime).TotalSeconds);

                    schedule.TimeSlots.Add(new BroadcastTimeSlot
                    {
                        BroadcastScheduleId = schedule.Id,
                        StartTime = slotDto.StartTime,
                        EndTime = slotDto.EndTime,
                        DurationSeconds = duration,
                        CreatedAt = DateTimeOffset.UtcNow
                    });
                }
            }

            foreach (var bd in schedule.BroadcastDevices)
            {
                bd.IsDeleted = true;
                bd.UpdatedAt = DateTimeOffset.UtcNow;
            }

            foreach (var devId in targetDeviceIds)
            {
                schedule.BroadcastDevices.Add(new BroadcastDevice
                {
                    BroadcastScheduleId = schedule.Id,
                    DeviceId = devId,
                    DeviceStatus = "Chờ phát",
                    CreatedAt = DateTimeOffset.UtcNow
                });
            }

            await _context.SaveChangesAsync();

            if (dto.Overwrite || isPriority)
            {
                await PreemptConflictingDevicesAsync(schedule.BroadcastId);
            }

            var allSchedules = await GetSchedulesAsync(schedule.BroadcastId);
            var updatedSchedule = allSchedules.Data?.FirstOrDefault(s => s.Id == schedule.Id);
            return ApiResponse<BroadcastScheduleResponseDto>.Ok(updatedSchedule!);
        }

        public async Task<ApiResponse<bool>> DeleteScheduleAsync(Guid scheduleId)
        {
            var schedule = await _context.BroadcastSchedules
                .Include(s => s.TimeSlots)
                .Include(s => s.BroadcastDevices)
                .FirstOrDefaultAsync(s => s.Id == scheduleId && !s.IsDeleted);

            if (schedule == null)
            {
                return ApiResponse<bool>.Fail("Không tìm thấy lịch phát!");
            }

            if (schedule.IsLocked)
            {
                return ApiResponse<bool>.Fail("Không thể xóa lịch phát đang bị khóa!");
            }

            schedule.IsDeleted = true;
            schedule.UpdatedAt = DateTimeOffset.UtcNow;

            foreach (var slot in schedule.TimeSlots)
            {
                slot.IsDeleted = true;
                slot.UpdatedAt = DateTimeOffset.UtcNow;
            }

            foreach (var bd in schedule.BroadcastDevices)
            {
                bd.IsDeleted = true;
                bd.UpdatedAt = DateTimeOffset.UtcNow;
            }

            await _context.SaveChangesAsync();
            return ApiResponse<bool>.Ok(true, "Đã xóa lịch phát thành công!");
        }

        // =========================================================
        // THUẬT TOÁN QUÉT TRÙNG LỊCH HOÀN CHỈNH (CONFLICT DETECTION ENGINE)
        // Chế độ phát theo Cơ sở: Hiển thị tên cơ sở chứa thiết bị trùng và danh sách tất cả bản tin trùng
        // Chế độ phát theo Thiết bị: Hiển thị tên thiết bị trùng và danh sách các bản tin trùng trên thiết bị đó
        // Công thức giao cắt giờ: (NewStart < ExistingEnd) && (NewEnd > ExistingStart)
        // =========================================================
        private async Task<ConflictCheckResultDto> CheckConflictEngineAsync(
            Guid organizationId,
            Guid facilityId,
            List<Guid> deviceIds,
            DateTimeOffset startDate,
            DateTimeOffset endDate,
            List<CreateBroadcastTimeSlotDto> timeSlots,
            Guid? excludeBroadcastId,
            Guid? excludeScheduleId)
        {
            var broadcastMode = await GetBroadcastModeAsync(organizationId);
            var facility = await _context.Facilities.AsNoTracking().FirstOrDefaultAsync(f => f.Id == facilityId);

            var result = new ConflictCheckResultDto
            {
                HasConflict = false,
                BroadcastMode = broadcastMode,
                FacilityName = facility?.FacilityName,
                ConflictingBroadcasts = new List<ConflictingBroadcastItemDto>()
            };

            if (deviceIds == null || !deviceIds.Any() || timeSlots == null || !timeSlots.Any())
            {
                return result;
            }

            var activeStatuses = new[] { "Chưa phát", "Sắp tới lịch", "Đang phát", "Chờ phát" };

            // Lấy tất cả lịch phát đang hiệu lực trên danh sách thiết bị cần quét
            var existingQuery = _context.BroadcastDevices
                .AsNoTracking()
                .Where(bd => !bd.IsDeleted && deviceIds.Contains(bd.DeviceId) && !bd.IsPreempted)
                .Join(_context.BroadcastSchedules.Where(s => !s.IsDeleted &&
                                                             (!excludeScheduleId.HasValue || s.Id != excludeScheduleId.Value) &&
                                                             (!excludeBroadcastId.HasValue || s.BroadcastId != excludeBroadcastId.Value)),
                      bd => bd.BroadcastScheduleId,
                      s => s.Id,
                      (bd, s) => new { bd.DeviceId, Schedule = s })
                .Join(_context.Broadcasts.Where(b => !b.IsDeleted && activeStatuses.Contains(b.Status)),
                      x => x.Schedule.BroadcastId,
                      b => b.Id,
                      (x, b) => new { x.DeviceId, x.Schedule, Broadcast = b });

            var existingList = await existingQuery.ToListAsync();
            if (!existingList.Any()) return result;

            var existingScheduleIds = existingList.Select(e => e.Schedule.Id).Distinct().ToList();
            var existingSlots = await _context.BroadcastTimeSlots
                .AsNoTracking()
                .Where(t => !t.IsDeleted && existingScheduleIds.Contains(t.BroadcastScheduleId))
                .ToListAsync();

            var devices = await _context.Devices
                .AsNoTracking()
                .Where(d => deviceIds.Contains(d.Id))
                .ToDictionaryAsync(d => d.Id, d => (d.DeviceName, d.DeviceCode));

            var conflictingItems = new List<ConflictingBroadcastItemDto>();

            foreach (var existing in existingList)
            {
                // Kiểm tra giao thoa khoảng ngày: (startDate <= existing.EndDate && endDate >= existing.StartDate)
                if (startDate.Date <= existing.Schedule.EndDate.Date && endDate.Date >= existing.Schedule.StartDate.Date)
                {
                    var slots = existingSlots.Where(s => s.BroadcastScheduleId == existing.Schedule.Id).ToList();
                    foreach (var eSlot in slots)
                    {
                        foreach (var newSlot in timeSlots)
                        {
                            // Công thức giao cắt giờ: (NewStart < ExistingEnd) && (NewEnd > ExistingStart)
                            if (newSlot.StartTime < eSlot.EndTime && newSlot.EndTime > eSlot.StartTime)
                            {
                                devices.TryGetValue(existing.DeviceId, out var dInfo);

                                var overlapStart = newSlot.StartTime > eSlot.StartTime ? newSlot.StartTime : eSlot.StartTime;
                                var overlapEnd = newSlot.EndTime < eSlot.EndTime ? newSlot.EndTime : eSlot.EndTime;
                                int durationMinutes = (int)(overlapEnd - overlapStart).TotalMinutes;

                                conflictingItems.Add(new ConflictingBroadcastItemDto
                                {
                                    BroadcastId = existing.Broadcast.Id,
                                    BroadcastCode = existing.Broadcast.BroadcastCode,
                                    BroadcastName = existing.Broadcast.BroadcastName,
                                    BroadcastType = existing.Broadcast.BroadcastType,
                                    Priority = existing.Broadcast.Priority,
                                    DeviceId = existing.DeviceId,
                                    DeviceName = dInfo.DeviceName,
                                    DeviceCode = dInfo.DeviceCode,
                                    FacilityName = facility?.FacilityName,
                                    StartTime = eSlot.StartTime,
                                    EndTime = eSlot.EndTime,
                                    ConflictStartDate = startDate > existing.Schedule.StartDate ? startDate : existing.Schedule.StartDate,
                                    ConflictEndDate = endDate < existing.Schedule.EndDate ? endDate : existing.Schedule.EndDate,
                                    ConflictDurationMinutes = durationMinutes > 0 ? durationMinutes : 1
                                });
                            }
                        }
                    }
                }
            }

            if (!conflictingItems.Any()) return result;

            result.HasConflict = true;
            result.ConflictingBroadcasts = conflictingItems;

            var distinctBroadcastNames = conflictingItems.Select(c => c.BroadcastName).Distinct().ToList();
            result.TotalConflictingBroadcasts = distinctBroadcastNames.Count;

            // Định dạng chuỗi thông báo theo đúng nghiệp vụ yêu cầu:
            if (broadcastMode == "Phát theo cơ sở")
            {
                var distinctDeviceCount = conflictingItems.Select(c => c.DeviceId).Distinct().Count();
                result.SummaryMessage = $"Cơ sở '{facility?.FacilityName ?? "Chỉ định"}' có {distinctDeviceCount} thiết bị trùng lịch phát với {distinctBroadcastNames.Count} bản tin: " +
                                        $"{string.Join(", ", distinctBroadcastNames.Select(n => $"'{n}'"))}. " +
                                        $"Vui lòng kiểm tra lại khung giờ hoặc sử dụng nút [Ghi đè]!";
            }
            else
            {
                var groupedByDevice = conflictingItems
                    .GroupBy(c => c.DeviceName ?? c.DeviceId.ToString())
                    .Select(g => $"Thiết bị '{g.Key}' trùng với {g.Select(x => x.BroadcastName).Distinct().Count()} bản tin ({string.Join(", ", g.Select(x => $"'{x.BroadcastName}'").Distinct())})")
                    .ToList();

                result.SummaryMessage = $"Phát hiện trùng lịch trên các thiết bị: {string.Join("; ", groupedByDevice)}. " +
                                        $"Vui lòng kiểm tra lại khung giờ hoặc sử dụng nút [Ghi đè]!";
            }

            return result;
        }

        // =========================================================
        // LẤY CHẾ ĐỘ PHÁT TỔ CHỨC: "Phát theo cơ sở" | "Phát theo thiết bị"
        // =========================================================
        private async Task<string> GetBroadcastModeAsync(Guid organizationId)
        {
            var setting = await _context.SystemSettings
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.OrganizationId == organizationId && s.SettingKey == KEY_BROADCAST_MODE && !s.IsDeleted);

            return setting?.SettingValue ?? "Phát theo cơ sở";
        }

        // =========================================================
        // TIỀN TRẢM / GHI ĐÈ THIẾT BỊ KHI CÓ BẢN TIN ƯU TIÊN HOẶC OVERWRITE = TRUE
        // =========================================================
        private async Task PreemptConflictingDevicesAsync(Guid currentBroadcastId)
        {
            var currentSchedules = await _context.BroadcastSchedules
                .Where(s => s.BroadcastId == currentBroadcastId && !s.IsDeleted)
                .Include(s => s.TimeSlots.Where(t => !t.IsDeleted))
                .Include(s => s.BroadcastDevices.Where(bd => !bd.IsDeleted))
                .ToListAsync();

            if (!currentSchedules.Any()) return;

            var targetDeviceIds = currentSchedules.SelectMany(s => s.BroadcastDevices).Select(bd => bd.DeviceId).Distinct().ToList();

            var activeStatuses = new[] { "Chưa phát", "Sắp tới lịch", "Đang phát", "Chờ phát" };
            var conflictingDevices = await _context.BroadcastDevices
                .Where(bd => !bd.IsDeleted &&
                             targetDeviceIds.Contains(bd.DeviceId) &&
                             !bd.IsPreempted)
                .Join(_context.BroadcastSchedules.Where(s => s.BroadcastId != currentBroadcastId && !s.IsDeleted),
                      bd => bd.BroadcastScheduleId,
                      s => s.Id,
                      (bd, s) => new { BroadcastDevice = bd, Schedule = s })
                .Join(_context.Broadcasts.Where(b => !b.IsDeleted && activeStatuses.Contains(b.Status)),
                      x => x.Schedule.BroadcastId,
                      b => b.Id,
                      (x, b) => new { x.BroadcastDevice, x.Schedule, Broadcast = b })
                .ToListAsync();

            foreach (var curSch in currentSchedules)
            {
                foreach (var conf in conflictingDevices)
                {
                    if (curSch.StartDate.Date <= conf.Schedule.EndDate.Date && curSch.EndDate.Date >= conf.Schedule.StartDate.Date)
                    {
                        conf.BroadcastDevice.IsPreempted = true;
                        conf.BroadcastDevice.PreemptedByBroadcastId = currentBroadcastId;
                        conf.BroadcastDevice.UpdatedAt = DateTimeOffset.UtcNow;
                    }
                }
            }

            await _context.SaveChangesAsync();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MTC_System.Common;
using MTC_System.Data;
using MTC_System.DTOs;
using MTC_System.Hubs;
using MTC_System.Models;
using MTC_System.Realtime;

namespace MTC_System.Services
{
    public class BroadcastService : IBroadcastService
    {
        private const string KEY_BROADCAST_MODE = "BROADCAST_MODE";
        public const string PLAY_MODE_IMMEDIATE = "Phát ngay sau khi phê duyệt";
        public const string PLAY_MODE_SCHEDULED = "Phát theo lịch";
        private const string TAG_PLAY_MODE_IMMEDIATE = "[Phát ngay sau khi phê duyệt]";

        private readonly MyDbContext _context;
        private readonly IBroadcastRealtimeDispatcher? _realtimeDispatcher;
        private readonly IHubContext<DeviceHub>? _hubContext;
        private readonly IDeviceConnectionManager? _connectionManager;
        private readonly IDeviceCommandManager? _commandManager;
        private readonly ILogger<BroadcastService>? _logger;

        public BroadcastService(
            MyDbContext context,
            IBroadcastRealtimeDispatcher? realtimeDispatcher = null,
            IHubContext<DeviceHub>? hubContext = null,
            IDeviceConnectionManager? connectionManager = null,
            IDeviceCommandManager? commandManager = null,
            ILogger<BroadcastService>? logger = null)
        {
            _context = context;
            _realtimeDispatcher = realtimeDispatcher;
            _hubContext = hubContext;
            _connectionManager = connectionManager;
            _commandManager = commandManager;
            _logger = logger;
        }

        // =========================================================
        // USR-BRD-01: XEM DANH SÁCH BẢN TIN (Tính toán phân quyền & Cha-Con)
        // =========================================================
        public async Task<ApiResponse<PagedResult<BroadcastListResponseDto>>> GetListAsync(
            Guid? facilityId,
            Guid? organizationId,
            string? status,
            string? broadcastType,
            string? search,
            int pageIndex = 1,
            int pageSize = 10,
            Guid? requestingFacilityId = null,
            Guid? deviceId = null,
            DateTimeOffset? startDateFrom = null,
            DateTimeOffset? startDateTo = null,
            string? broadcastForm = null)
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

            if (!string.IsNullOrWhiteSpace(status) && status.Trim() != "Tất cả")
            {
                query = query.Where(b => b.Status == status.Trim());
            }

            if (!string.IsNullOrWhiteSpace(broadcastType) && broadcastType.Trim() != "Tất cả")
            {
                var typeTrim = broadcastType.Trim();
                if (typeTrim.Equals("Tin thường", StringComparison.OrdinalIgnoreCase) || typeTrim.Equals("Thường", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(b => b.BroadcastType == "Thường" && b.Priority != 1);
                }
                else if (typeTrim.Equals("Tin ưu tiên", StringComparison.OrdinalIgnoreCase) || typeTrim.Equals("Ưu tiên", StringComparison.OrdinalIgnoreCase) || typeTrim.Equals("Khẩn cấp", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(b => b.BroadcastType == "Khẩn cấp" || b.BroadcastType == "Ưu tiên" || b.Priority == 1);
                }
                else
                {
                    query = query.Where(b => b.BroadcastType == typeTrim);
                }
            }

            if (deviceId.HasValue && deviceId.Value != Guid.Empty)
            {
                var targetDevId = deviceId.Value;
                query = query.Where(b => b.Schedules.Any(s => !s.IsDeleted && s.BroadcastDevices.Any(bd => !bd.IsDeleted && bd.DeviceId == targetDevId)));
            }

            if (startDateFrom.HasValue)
            {
                var fromDate = new DateTimeOffset(startDateFrom.Value.UtcDateTime.Date, TimeSpan.Zero);
                query = query.Where(b => b.Schedules.Any(s => !s.IsDeleted && s.StartDate >= fromDate));
            }

            if (startDateTo.HasValue)
            {
                var toDate = new DateTimeOffset(startDateTo.Value.UtcDateTime.Date.AddDays(1), TimeSpan.Zero);
                query = query.Where(b => b.Schedules.Any(s => !s.IsDeleted && s.StartDate < toDate));
            }

            if (!string.IsNullOrWhiteSpace(broadcastForm) && broadcastForm.Trim() != "Tất cả")
            {
                var formTrim = broadcastForm.Trim();
                if (formTrim.Equals("Bản tin nhanh", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(b => b.Description != null && b.Description.Contains(TAG_PLAY_MODE_IMMEDIATE));
                }
                else if (formTrim.Equals("Bản tin tiêu chuẩn", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(b => b.Description == null || !b.Description.Contains(TAG_PLAY_MODE_IMMEDIATE));
                }
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                query = query.Where(b => b.BroadcastCode.ToLower().Contains(term) ||
                                         b.BroadcastName.ToLower().Contains(term) ||
                                         (b.Description != null && b.Description.ToLower().Contains(term)));
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
                    b.CreatedBy,
                    b.CreatedAt,
                    TotalSchedules = b.Schedules.Count(s => !s.IsDeleted),
                    FirstSchedule = b.Schedules
                        .Where(s => !s.IsDeleted)
                        .OrderBy(s => s.StartDate)
                        .Select(s => new
                        {
                            s.StartDate,
                            s.EndDate,
                            TimeSlots = s.TimeSlots
                                .Where(ts => !ts.IsDeleted)
                                .OrderBy(ts => ts.StartTime)
                                .Select(ts => new
                                {
                                    ts.StartTime,
                                    ts.EndTime
                                })
                                .ToList()
                        })
                        .FirstOrDefault()
                })
                .ToListAsync();

            var facilityIds = broadcasts.Select(i => i.FacilityId).Distinct().ToList();
            var facilityMap = await _context.Facilities
                .AsNoTracking()
                .Where(f => facilityIds.Contains(f.Id))
                .ToDictionaryAsync(f => f.Id, f => (f.FacilityName, f.ParentId));

            var creatorIds = broadcasts
                .Where(b => b.CreatedBy.HasValue)
                .Select(b => b.CreatedBy!.Value)
                .Distinct()
                .ToList();

            var userMap = new Dictionary<Guid, string>();
            if (creatorIds.Any())
            {
                userMap = await _context.Users
                    .AsNoTracking()
                    .Where(u => creatorIds.Contains(u.Id))
                    .ToDictionaryAsync(u => u.Id, u => u.FullName);
            }

            var ancestorFacilityIds = new HashSet<Guid>();
            if (requestingFacilityId.HasValue && requestingFacilityId.Value != Guid.Empty)
            {
                ancestorFacilityIds = await GetAncestorFacilityIdsAsync(requestingFacilityId.Value);
            }

            var items = broadcasts.Select(b =>
            {
                bool isFromParent = requestingFacilityId.HasValue &&
                                    requestingFacilityId.Value != Guid.Empty &&
                                    b.FacilityId != requestingFacilityId.Value &&
                                    ancestorFacilityIds.Contains(b.FacilityId);

                bool isCurrentFacility = !requestingFacilityId.HasValue || requestingFacilityId.Value == b.FacilityId;
                bool isPriority = b.Priority == 1 || b.BroadcastType == "Ưu tiên" || b.BroadcastType == "Khẩn cấp";

                // USR-BRD-01: Bản tin do cơ sở cha tạo chỉ cho cơ sở con xem chi tiết, không sửa
                bool canEdit = isCurrentFacility && !isFromParent && b.Status == "Chờ duyệt";
                bool canDelete = isCurrentFacility && !isFromParent && (b.Status == "Chờ duyệt" || b.Status == "Từ chối duyệt" || b.Status == "Đã hủy");
                bool canApprove = !isFromParent && b.Status == "Chờ duyệt";
                bool canReject = !isFromParent && b.Status == "Chờ duyệt";
                bool canCancel = isCurrentFacility && !isFromParent && (b.Status == "Chưa phát" || b.Status == "Sắp tới lịch" || b.Status == "Đang phát" || b.Status == "Chờ phát" || b.Status == "Tạm ngưng" || b.Status == "Đã duyệt");
                bool canPause = isCurrentFacility && !isFromParent && b.Status == "Đang phát" && !isPriority;
                bool canResume = isCurrentFacility && !isFromParent && b.Status == "Tạm ngưng" && !isPriority;

                facilityMap.TryGetValue(b.FacilityId, out var fInfo);

                var isImmediate = (b.Description != null && b.Description.Contains(TAG_PLAY_MODE_IMMEDIATE));
                var playMode = isImmediate ? PLAY_MODE_IMMEDIATE : PLAY_MODE_SCHEDULED;
                var broadcastForm = isImmediate ? "Bản tin nhanh" : "Bản tin tiêu chuẩn";
                var broadcastTypeDisplay = isPriority ? "Tin ưu tiên" : "Tin thường";

                var cleanDesc = b.Description?.Replace(TAG_PLAY_MODE_IMMEDIATE, "").Trim();

                string broadcastDates = "-";
                string broadcastTimes = "-";
                if (b.FirstSchedule != null)
                {
                    broadcastDates = $"{b.FirstSchedule.StartDate:dd/MM/yyyy} - {b.FirstSchedule.EndDate:dd/MM/yyyy}";
                    if (b.FirstSchedule.TimeSlots != null && b.FirstSchedule.TimeSlots.Any())
                    {
                        broadcastTimes = string.Join(", ", b.FirstSchedule.TimeSlots.Select(ts => $"{ts.StartTime:hh\\:mm\\:ss} - {ts.EndTime:hh\\:mm\\:ss}"));
                    }
                }

                string createdByName = "Nguyễn Văn A";
                if (b.CreatedBy.HasValue && userMap.TryGetValue(b.CreatedBy.Value, out var uName) && !string.IsNullOrWhiteSpace(uName))
                {
                    createdByName = uName;
                }

                return new BroadcastListResponseDto
                {
                    Id = b.Id,
                    BroadcastCode = b.BroadcastCode,
                    BroadcastName = b.BroadcastName,
                    Description = cleanDesc,
                    BroadcastType = b.BroadcastType,
                    Priority = b.Priority,
                    Status = b.Status,
                    PlayMode = playMode,
                    ContentType = b.ContentType,
                    OrganizationId = b.OrganizationId,
                    FacilityId = b.FacilityId,
                    FacilityName = fInfo.FacilityName,
                    ApprovedAt = b.ApprovedAt,
                    ApprovedBy = b.ApprovedBy,
                    CreatedBy = b.CreatedBy,
                    CreatedByName = createdByName,
                    CreatedAt = b.CreatedAt,
                    TotalSchedules = b.TotalSchedules,
                    BroadcastForm = broadcastForm,
                    BroadcastTypeDisplay = broadcastTypeDisplay,
                    BroadcastDates = broadcastDates,
                    BroadcastTimes = broadcastTimes,
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
            int playlistDuration = 0;
            if (broadcast.PlaylistId.HasValue)
            {
                var pl = await _context.Playlists.AsNoTracking().FirstOrDefaultAsync(p => p.Id == broadcast.PlaylistId.Value);
                playlistName = pl?.PlaylistName;
                playlistDuration = pl?.TotalDurationSeconds ?? 0;
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
                var ancestors = await GetAncestorFacilityIdsAsync(requestingFacilityId.Value);
                if (ancestors.Contains(broadcast.FacilityId))
                {
                    isFromParent = true;
                }
            }

            bool isCurrentFacility = !requestingFacilityId.HasValue || requestingFacilityId.Value == broadcast.FacilityId;
            bool isPriority = broadcast.Priority == 1 || broadcast.BroadcastType == "Ưu tiên" || broadcast.BroadcastType == "Khẩn cấp";

            var playMode = (broadcast.Description != null && broadcast.Description.Contains(TAG_PLAY_MODE_IMMEDIATE))
                ? PLAY_MODE_IMMEDIATE
                : PLAY_MODE_SCHEDULED;
            var cleanDesc = broadcast.Description?.Replace(TAG_PLAY_MODE_IMMEDIATE, "").Trim();

            bool isDurationLocked = broadcast.ContentType == "Playlist";

            var dto = new BroadcastDetailResponseDto
            {
                Id = broadcast.Id,
                BroadcastCode = broadcast.BroadcastCode,
                BroadcastName = broadcast.BroadcastName,
                Description = cleanDesc,
                BroadcastType = broadcast.BroadcastType,
                Priority = broadcast.Priority,
                Status = broadcast.Status,
                PlayMode = playMode,
                IsDurationLocked = isDurationLocked,
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
                CanApprove = !isFromParent && broadcast.Status == "Chờ duyệt",
                CanReject = !isFromParent && broadcast.Status == "Chờ duyệt",
                CanCancel = isCurrentFacility && !isFromParent && (broadcast.Status == "Chưa phát" || broadcast.Status == "Sắp tới lịch" || broadcast.Status == "Đang phát" || broadcast.Status == "Chờ phát" || broadcast.Status == "Tạm ngưng" || broadcast.Status == "Đã duyệt"),
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
                    TimeSlots = s.TimeSlots.Select(t =>
                    {
                        int repCount = 1;
                        if (isDurationLocked && playlistDuration > 0 && t.DurationSeconds > 0)
                        {
                            repCount = Math.Max(1, (int)Math.Round((double)t.DurationSeconds / playlistDuration));
                        }
                        return new BroadcastTimeSlotResponseDto
                        {
                            Id = t.Id,
                            BroadcastScheduleId = t.BroadcastScheduleId,
                            StartTime = t.StartTime,
                            EndTime = t.EndTime,
                            DurationSeconds = t.DurationSeconds,
                            RepeatCount = repCount
                        };
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
        // USR-BRD-02: THÊM BẢN TIN (Quét trùng lịch theo Chế độ Cơ sở / Thiết bị, khóa thời lượng Playlist)
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

            bool isPriority = dto.Priority == 1 || dto.BroadcastType == "Ưu tiên" || dto.BroadcastType == "Khẩn cấp";

            // KIỂM TRA QUY TẮC: Bản tin thường không được phép phát qua ngày, chỉ bản tin ưu tiên mới được phát qua ngày
            if (dto.Schedules != null && dto.Schedules.Any())
            {
                foreach (var sch in dto.Schedules)
                {
                    if (sch.EndDate < sch.StartDate)
                    {
                        return ApiResponse<BroadcastDetailResponseDto>.Fail("Ngày kết thúc phát phải lớn hơn hoặc bằng ngày bắt đầu phát!");
                    }

                    if (!isPriority)
                    {
                        if (sch.StartDate.Date != sch.EndDate.Date)
                        {
                            return ApiResponse<BroadcastDetailResponseDto>.Fail(
                                $"Bản tin thường không được phép phát qua ngày (Từ {sch.StartDate:yyyy-MM-dd} đến {sch.EndDate:yyyy-MM-dd}). Chỉ bản tin ưu tiên mới được phép phát qua ngày!");
                        }

                        if (sch.TimeSlots != null)
                        {
                            foreach (var slot in sch.TimeSlots)
                            {
                                if (slot.EndTime <= slot.StartTime)
                                {
                                    return ApiResponse<BroadcastDetailResponseDto>.Fail(
                                        $"Bản tin thường không được cấu hình khung giờ phát qua đêm hoặc giờ kết thúc nhỏ hơn/bằng giờ bắt đầu ({slot.StartTime} - {slot.EndTime})!");
                                }
                            }
                        }
                    }
                }
            }

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

            // QUÉT TRÙNG LỊCH (CONFLICT DETECTION)
            var allConflictingItems = new List<ConflictingBroadcastItemDto>();
            if (dto.Schedules != null && dto.Schedules.Any())
            {
                foreach (var sch in dto.Schedules)
                {
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
                            sch.RepeatType,
                            sch.RepeatDays,
                            sch.TimeSlots,
                            null,
                            null);

                        if (conflictResult.HasConflict)
                        {
                            allConflictingItems.AddRange(conflictResult.ConflictingBroadcasts);
                        }
                    }
                }
            }

            if (allConflictingItems.Any())
            {
                var conflictingBroadcastIds = allConflictingItems.Select(c => c.BroadcastId).Distinct().ToList();

                if (!dto.Overwrite)
                {
                    // Trả về lỗi chi tiết trùng lịch
                    var distinctNames = allConflictingItems.Select(c => c.BroadcastName).Distinct().ToList();
                    string summaryMsg = (broadcastMode == "Phát theo cơ sở")
                        ? $"Cơ sở '{facility.FacilityName}' có {allConflictingItems.Select(c => c.DeviceId).Distinct().Count()} thiết bị trùng lịch phát với {distinctNames.Count} bản tin: {string.Join(", ", distinctNames.Select(n => $"'{n}'"))}. Vui lòng kiểm tra lại khung giờ hoặc sử dụng nút [Ghi đè]!"
                        : $"Phát hiện trùng lịch trên các thiết bị: {string.Join("; ", allConflictingItems.Select(c => $"Thiết bị '{c.DeviceName}' trùng với bản tin '{c.BroadcastName}' lúc {c.NearestConflictTime:dd/MM/yyyy HH:mm} ({c.NearestConflictDurationMinutes} phút)").Distinct())}. Vui lòng kiểm tra lại khung giờ hoặc sử dụng nút [Ghi đè]!";

                    var errorList = allConflictingItems
                        .Select(c => $"Bản tin '{c.BroadcastName}' ({c.BroadcastCode}) - {(broadcastMode == "Phát theo cơ sở" ? $"Cơ sở: {c.FacilityName}" : $"Thiết bị: {c.DeviceName}")} [{c.StartTime:hh\\:mm} - {c.EndTime:hh\\:mm}]")
                        .Distinct()
                        .ToList();

                    return ApiResponse<BroadcastDetailResponseDto>.Fail(summaryMsg, errorList);
                }

                // XỬ LÝ CỜ OVERWRITE = TRUE:
                // Áp dụng cho: Bản tin ưu tiên HOẶC khi bản tin cơ sở cha ghi đè bản tin cơ sở con (ở cả tin thường và tin ưu tiên)
                bool isParentOverridingChild = await IsParentOverridingChildBroadcastsAsync(dto.FacilityId, conflictingBroadcastIds);

                if (!isPriority && !isParentOverridingChild)
                {
                    return ApiResponse<BroadcastDetailResponseDto>.Fail("Chỉ bản tin ưu tiên hoặc bản tin của cơ sở cha mới được phép ghi đè lịch phát sóng (Overwrite)!");
                }
            }

            // Xây dựng Description kèm tag PlayMode nếu chọn Phát ngay sau khi phê duyệt
            string description = dto.Description?.Trim() ?? string.Empty;
            if (dto.PlayMode == PLAY_MODE_IMMEDIATE && !description.Contains(TAG_PLAY_MODE_IMMEDIATE))
            {
                description = $"{TAG_PLAY_MODE_IMMEDIATE} {description}".Trim();
            }

            var broadcast = new Broadcast
            {
                BroadcastCode = code,
                BroadcastName = dto.BroadcastName.Trim(),
                Description = string.IsNullOrWhiteSpace(description) ? null : description,
                BroadcastType = isPriority ? (dto.BroadcastType == "Khẩn cấp" ? "Khẩn cấp" : "Ưu tiên") : "Thường",
                Priority = isPriority ? 1 : 2,
                Status = "Chờ duyệt", // Mặc định khởi tạo luôn là "Chờ duyệt"
                ContentType = dto.ContentType.Trim(),
                MediaId = dto.ContentType == "File" ? dto.MediaId : null,
                PlaylistId = dto.ContentType == "Playlist" ? dto.PlaylistId : null,
                OrganizationId = dto.OrganizationId,
                FacilityId = dto.FacilityId,
                CreatedBy = dto.CreatedBy,
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
                            int repeatCount = slotDto.RepeatCount > 0 ? slotDto.RepeatCount : 1;
                            // USR-BRD-02: Khóa không cho chỉnh lại thời lượng nếu nguồn phát là Danh sách phát (Playlist)
                            int duration;
                            if (dto.ContentType == "Playlist" && playlistDuration > 0)
                            {
                                duration = playlistDuration * repeatCount;
                            }
                            else
                            {
                                duration = slotDto.DurationSeconds > 0
                                    ? slotDto.DurationSeconds * repeatCount
                                    : (int)(slotDto.EndTime - slotDto.StartTime).TotalSeconds * repeatCount;
                            }

                            schedule.TimeSlots.Add(new BroadcastTimeSlot
                            {
                                BroadcastScheduleId = schedule.Id,
                                StartTime = slotDto.StartTime,
                                EndTime = slotDto.EndTime,
                                DurationSeconds = Math.Max(0, duration),
                                CreatedAt = DateTimeOffset.UtcNow
                            });
                        }
                    }

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

            // Thực hiện ghi đè & gửi thông báo NTF-09 cho người tạo bản tin bị đè
            if (dto.Overwrite && allConflictingItems.Any())
            {
                var conflictingBroadcastIds = allConflictingItems.Select(c => c.BroadcastId).Distinct().ToList();
                await PreemptAndNotifyOverwrittenBroadcastsAsync(broadcast, conflictingBroadcastIds);
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
                dto.RepeatType,
                dto.RepeatDays,
                dto.TimeSlots,
                dto.ExcludeBroadcastId,
                null);

            return ApiResponse<ConflictCheckResultDto>.Ok(result);
        }

        // =========================================================
        // USR-BRD-04: CHỈNH SỬA (Chỉ sửa bản tin Chờ duyệt, khóa thời lượng Playlist)
        // =========================================================
        public async Task<ApiResponse<BroadcastDetailResponseDto>> UpdateAsync(Guid id, UpdateBroadcastDto dto, Guid? requestingFacilityId = null)
        {
            var broadcast = await _context.Broadcasts.FirstOrDefaultAsync(b => b.Id == id && !b.IsDeleted);
            if (broadcast == null)
            {
                return ApiResponse<BroadcastDetailResponseDto>.Fail("Không tìm thấy bản tin!");
            }

            // KIỂM TRA QUY TẮC: "Chỉ sửa bản tin Chờ duyệt"
            if (broadcast.Status != "Chờ duyệt")
            {
                return ApiResponse<BroadcastDetailResponseDto>.Fail($"Không thể chỉnh sửa bản tin khi đang ở trạng thái '{broadcast.Status}'. Chỉ sửa bản tin 'Chờ duyệt'!");
            }

            // USR-BRD-01: Bản tin do cơ sở cha tạo chỉ cho cơ sở con xem chi tiết, không sửa
            if (requestingFacilityId.HasValue && requestingFacilityId.Value != Guid.Empty && requestingFacilityId.Value != broadcast.FacilityId)
            {
                var ancestors = await GetAncestorFacilityIdsAsync(requestingFacilityId.Value);
                if (ancestors.Contains(broadcast.FacilityId))
                {
                    return ApiResponse<BroadcastDetailResponseDto>.Fail("Bản tin do cơ sở cha tạo chỉ cho phép xem chi tiết, không được chỉnh sửa (USR-BRD-01)!");
                }
            }

            bool isPriority = dto.Priority == 1 || dto.BroadcastType == "Ưu tiên" || dto.BroadcastType == "Khẩn cấp";

            int playlistDuration = 0;
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
                var playlist = await _context.Playlists.FirstOrDefaultAsync(p => p.Id == dto.PlaylistId.Value && !p.IsDeleted);
                if (playlist == null)
                    return ApiResponse<BroadcastDetailResponseDto>.Fail("Playlist được chọn không tồn tại!");
                playlistDuration = playlist.TotalDurationSeconds;
            }

            // Kiểm tra lịch phát nếu có cập nhật
            if (dto.Schedules != null && dto.Schedules.Any())
            {
                foreach (var sch in dto.Schedules)
                {
                    if (sch.EndDate < sch.StartDate)
                    {
                        return ApiResponse<BroadcastDetailResponseDto>.Fail("Ngày kết thúc phát phải lớn hơn hoặc bằng ngày bắt đầu phát!");
                    }

                    if (!isPriority)
                    {
                        if (sch.StartDate.Date != sch.EndDate.Date)
                        {
                            return ApiResponse<BroadcastDetailResponseDto>.Fail(
                                $"Bản tin thường không được phép phát qua ngày (Từ {sch.StartDate:yyyy-MM-dd} đến {sch.EndDate:yyyy-MM-dd}). Chỉ bản tin ưu tiên mới được phép phát qua ngày!");
                        }

                        if (sch.TimeSlots != null)
                        {
                            foreach (var slot in sch.TimeSlots)
                            {
                                if (slot.EndTime <= slot.StartTime)
                                {
                                    return ApiResponse<BroadcastDetailResponseDto>.Fail(
                                        $"Bản tin thường không được cấu hình khung giờ phát qua đêm hoặc giờ kết thúc nhỏ hơn/bằng giờ bắt đầu ({slot.StartTime} - {slot.EndTime})!");
                                }
                            }
                        }
                    }
                }
            }

            string description = dto.Description?.Trim() ?? string.Empty;
            if (dto.PlayMode == PLAY_MODE_IMMEDIATE && !description.Contains(TAG_PLAY_MODE_IMMEDIATE))
            {
                description = $"{TAG_PLAY_MODE_IMMEDIATE} {description}".Trim();
            }
            else if (dto.PlayMode == PLAY_MODE_SCHEDULED)
            {
                description = description.Replace(TAG_PLAY_MODE_IMMEDIATE, "").Trim();
            }

            broadcast.BroadcastName = dto.BroadcastName.Trim();
            broadcast.Description = string.IsNullOrWhiteSpace(description) ? null : description;
            broadcast.BroadcastType = isPriority ? (dto.BroadcastType == "Khẩn cấp" ? "Khẩn cấp" : "Ưu tiên") : "Thường";
            broadcast.Priority = isPriority ? 1 : 2;
            broadcast.ContentType = dto.ContentType.Trim();
            broadcast.MediaId = dto.ContentType == "File" ? dto.MediaId : null;
            broadcast.PlaylistId = dto.ContentType == "Playlist" ? dto.PlaylistId : null;
            broadcast.UpdatedAt = DateTimeOffset.UtcNow;

            await _context.SaveChangesAsync();
            return await GetByIdAsync(broadcast.Id, requestingFacilityId);
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

            // USR-BRD-01: Chặn cơ sở con xóa bản tin cha
            if (requestingFacilityId.HasValue && requestingFacilityId.Value != Guid.Empty && requestingFacilityId.Value != broadcast.FacilityId)
            {
                var ancestors = await GetAncestorFacilityIdsAsync(requestingFacilityId.Value);
                if (ancestors.Contains(broadcast.FacilityId))
                {
                    return ApiResponse<bool>.Fail("Bản tin do cơ sở cha tạo chỉ cho phép xem chi tiết, không được phép xóa (USR-BRD-01)!");
                }
            }

            // KIỂM TRA QUY TẮC: "chỉ xóa bản tin Chờ duyệt, Từ chối duyệt, Đã hủy"
            var allowedStatuses = new[] { "Chờ duyệt", "Từ chối duyệt", "Đã hủy" };
            if (!allowedStatuses.Contains(broadcast.Status))
            {
                return ApiResponse<bool>.Fail($"Không thể xóa bản tin đang ở trạng thái '{broadcast.Status}'. Chỉ xóa bản tin Chờ duyệt, Từ chối duyệt hoặc Đã hủy!");
            }

            var now = DateTimeOffset.UtcNow;
            broadcast.IsDeleted = true;
            broadcast.UpdatedAt = now;

            foreach (var s in broadcast.Schedules)
            {
                s.IsDeleted = true;
                s.UpdatedAt = now;
                foreach (var ts in s.TimeSlots)
                {
                    ts.IsDeleted = true;
                    ts.UpdatedAt = now;
                }
                foreach (var bd in s.BroadcastDevices)
                {
                    bd.IsDeleted = true;
                    bd.UpdatedAt = now;
                }
            }

            await _context.SaveChangesAsync();
            return ApiResponse<bool>.Ok(true, "Xóa bản tin thành công!");
        }

        // =========================================================
        // USR-BRD-06: DUYỆT (Hỗ trợ Phát ngay sau khi phê duyệt hoặc Phát theo lịch, bản tin ưu tiên đè tin trước)
        // =========================================================
        public async Task<ApiResponse<BroadcastDetailResponseDto>> ApproveAsync(Guid id, ApproveBroadcastDto dto, Guid? requestingFacilityId = null)
        {
            var broadcast = await _context.Broadcasts
                .Include(b => b.Schedules)
                    .ThenInclude(s => s.BroadcastDevices)
                .FirstOrDefaultAsync(b => b.Id == id && !b.IsDeleted);

            if (broadcast == null)
            {
                return ApiResponse<BroadcastDetailResponseDto>.Fail("Không tìm thấy bản tin!");
            }

            // USR-BRD-01: Chặn cơ sở con duyệt bản tin cha
            if (requestingFacilityId.HasValue && requestingFacilityId.Value != Guid.Empty && requestingFacilityId.Value != broadcast.FacilityId)
            {
                var ancestors = await GetAncestorFacilityIdsAsync(requestingFacilityId.Value);
                if (ancestors.Contains(broadcast.FacilityId))
                {
                    return ApiResponse<BroadcastDetailResponseDto>.Fail("Bản tin do cơ sở cha tạo chỉ cho phép xem chi tiết, không được phép phê duyệt (USR-BRD-01)!");
                }
            }

            if (broadcast.Status != "Chờ duyệt")
            {
                return ApiResponse<BroadcastDetailResponseDto>.Fail($"Bản tin đang ở trạng thái '{broadcast.Status}'. Chỉ duyệt bản tin Chờ duyệt!");
            }

            bool isImmediate = broadcast.Description != null && broadcast.Description.Contains(TAG_PLAY_MODE_IMMEDIATE);

            // Chế độ phát: "Phát ngay sau khi phê duyệt" -> chuyển sang "Đang phát", ngược lại -> "Chưa phát"
            broadcast.Status = isImmediate ? "Đang phát" : "Chưa phát";
            broadcast.ApprovedAt = DateTimeOffset.UtcNow;
            broadcast.ApprovedBy = dto.ApprovedBy;
            broadcast.RejectionReason = null;
            broadcast.UpdatedAt = DateTimeOffset.UtcNow;

            // KIỂM TRA QUY TẮC: "Bản tin ưu tiên: duyệt sau phát đè lên bản tin duyệt trước"
            bool isPriority = broadcast.Priority == 1 || broadcast.BroadcastType == "Ưu tiên" || broadcast.BroadcastType == "Khẩn cấp";
            if (isPriority || dto.Overwrite)
            {
                await PreemptConflictingDevicesAsync(broadcast.Id);
            }

            await _context.SaveChangesAsync();

            // Kích hoạt realtime dispatcher phát sóng ngay tức thì nếu là chế độ "Phát ngay sau khi phê duyệt" hoặc là bản tin ưu tiên
            if (_realtimeDispatcher != null)
            {
                try
                {
                    await _realtimeDispatcher.DispatchBroadcastToDevicesAsync(broadcast.Id);
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Lỗi realtime dispatch sau khi duyệt bản tin {BroadcastId}", broadcast.Id);
                }
            }

            return await GetByIdAsync(broadcast.Id, requestingFacilityId);
        }

        // =========================================================
        // USR-BRD-07: TỪ CHỐI PHÊ DUYỆT (Chỉ với bản tin Chờ duyệt. Lý do tối đa 510 ký tự)
        // =========================================================
        public async Task<ApiResponse<BroadcastDetailResponseDto>> RejectAsync(Guid id, RejectBroadcastDto dto, Guid? requestingFacilityId = null)
        {
            var broadcast = await _context.Broadcasts.FirstOrDefaultAsync(b => b.Id == id && !b.IsDeleted);
            if (broadcast == null)
            {
                return ApiResponse<BroadcastDetailResponseDto>.Fail("Không tìm thấy bản tin!");
            }

            // USR-BRD-01: Chặn cơ sở con từ chối bản tin cha
            if (requestingFacilityId.HasValue && requestingFacilityId.Value != Guid.Empty && requestingFacilityId.Value != broadcast.FacilityId)
            {
                var ancestors = await GetAncestorFacilityIdsAsync(requestingFacilityId.Value);
                if (ancestors.Contains(broadcast.FacilityId))
                {
                    return ApiResponse<BroadcastDetailResponseDto>.Fail("Bản tin do cơ sở cha tạo chỉ cho phép xem chi tiết, không được phép thao tác (USR-BRD-01)!");
                }
            }

            if (broadcast.Status != "Chờ duyệt")
            {
                return ApiResponse<BroadcastDetailResponseDto>.Fail($"Bản tin đang ở trạng thái '{broadcast.Status}'. Chỉ từ chối bản tin Chờ duyệt!");
            }

            // KIỂM TRA QUY TẮC: "từ chối duyệt bắt buộc nhập lý do tối đa 510 ký tự"
            if (string.IsNullOrWhiteSpace(dto.RejectionReason))
            {
                return ApiResponse<BroadcastDetailResponseDto>.Fail("Bắt buộc phải nhập lý do từ chối!");
            }
            if (dto.RejectionReason.Trim().Length > 510)
            {
                return ApiResponse<BroadcastDetailResponseDto>.Fail($"Lý do từ chối tối đa 510 ký tự! Bạn đã nhập {dto.RejectionReason.Trim().Length} ký tự.");
            }

            broadcast.Status = "Từ chối duyệt";
            broadcast.RejectionReason = dto.RejectionReason.Trim();
            broadcast.ApprovedAt = DateTimeOffset.UtcNow;
            broadcast.ApprovedBy = dto.RejectedBy;
            broadcast.UpdatedAt = DateTimeOffset.UtcNow;

            await _context.SaveChangesAsync();
            return await GetByIdAsync(broadcast.Id, requestingFacilityId);
        }

        // =========================================================
        // USR-BRD-08: HỦY BẢN TIN (Chỉ hủy bản tin Đã duyệt)
        // =========================================================
        public async Task<ApiResponse<bool>> CancelAsync(Guid id, CancelBroadcastDto dto, Guid? requestingFacilityId = null)
        {
            var broadcast = await _context.Broadcasts
                .Include(b => b.Schedules)
                    .ThenInclude(s => s.BroadcastDevices)
                .FirstOrDefaultAsync(b => b.Id == id && !b.IsDeleted);

            if (broadcast == null)
            {
                return ApiResponse<bool>.Fail("Không tìm thấy bản tin!");
            }

            // USR-BRD-01: Chặn cơ sở con hủy bản tin cha
            if (requestingFacilityId.HasValue && requestingFacilityId.Value != Guid.Empty && requestingFacilityId.Value != broadcast.FacilityId)
            {
                var ancestors = await GetAncestorFacilityIdsAsync(requestingFacilityId.Value);
                if (ancestors.Contains(broadcast.FacilityId))
                {
                    return ApiResponse<bool>.Fail("Bản tin do cơ sở cha tạo chỉ cho phép xem chi tiết, không có quyền hủy (USR-BRD-01)!");
                }
            }

            // KIỂM TRA QUY TẮC: "chỉ hủy bản tin Đã duyệt"
            var approvedStatuses = new[] { "Chưa phát", "Sắp tới lịch", "Đang phát", "Chờ phát", "Tạm ngưng", "Đã duyệt" };
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

            // Dừng phát trên các thiết bị nếu đang phát
            await SendStopCommandToDevicesAsync(broadcast, "BroadcastCancelled");

            await _context.SaveChangesAsync();
            return ApiResponse<bool>.Ok(true, "Đã hủy bản tin thành công!");
        }

        // =========================================================
        // USR-BRD-09: DỪNG PHÁT (Chỉ với bản tin Đang phát; không áp dụng cho bản tin ưu tiên)
        // =========================================================
        public async Task<ApiResponse<BroadcastDetailResponseDto>> PauseAsync(Guid id, PauseBroadcastDto dto, Guid? requestingFacilityId = null)
        {
            var broadcast = await _context.Broadcasts
                .Include(b => b.Schedules)
                    .ThenInclude(s => s.BroadcastDevices)
                .FirstOrDefaultAsync(b => b.Id == id && !b.IsDeleted);

            if (broadcast == null)
            {
                return ApiResponse<BroadcastDetailResponseDto>.Fail("Không tìm thấy bản tin!");
            }

            // USR-BRD-01: Chặn cơ sở con dừng phát bản tin cha
            if (requestingFacilityId.HasValue && requestingFacilityId.Value != Guid.Empty && requestingFacilityId.Value != broadcast.FacilityId)
            {
                var ancestors = await GetAncestorFacilityIdsAsync(requestingFacilityId.Value);
                if (ancestors.Contains(broadcast.FacilityId))
                {
                    return ApiResponse<BroadcastDetailResponseDto>.Fail("Bản tin do cơ sở cha tạo chỉ cho phép xem chi tiết, không được phép thao tác (USR-BRD-01)!");
                }
            }

            // KIỂM TRA QUY TẮC: "Bản tin ưu tiên: không áp dụng dừng phát/tiếp tục phát"
            if (broadcast.Priority == 1 || broadcast.BroadcastType == "Ưu tiên" || broadcast.BroadcastType == "Khẩn cấp")
            {
                return ApiResponse<BroadcastDetailResponseDto>.Fail("Bản tin ưu tiên không áp dụng tính năng tạm dừng phát!");
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

            await SendStopCommandToDevicesAsync(broadcast, "BroadcastPaused");

            await _context.SaveChangesAsync();
            return await GetByIdAsync(broadcast.Id, requestingFacilityId);
        }

        // =========================================================
        // USR-BRD-10: TIẾP TỤC PHÁT (Chỉ với bản tin Tạm ngưng; không áp dụng cho bản tin ưu tiên)
        // =========================================================
        public async Task<ApiResponse<BroadcastDetailResponseDto>> ResumeAsync(Guid id, Guid? requestingFacilityId = null)
        {
            var broadcast = await _context.Broadcasts
                .Include(b => b.Schedules)
                    .ThenInclude(s => s.BroadcastDevices)
                .FirstOrDefaultAsync(b => b.Id == id && !b.IsDeleted);

            if (broadcast == null)
            {
                return ApiResponse<BroadcastDetailResponseDto>.Fail("Không tìm thấy bản tin!");
            }

            // USR-BRD-01: Chặn cơ sở con tiếp tục phát bản tin cha
            if (requestingFacilityId.HasValue && requestingFacilityId.Value != Guid.Empty && requestingFacilityId.Value != broadcast.FacilityId)
            {
                var ancestors = await GetAncestorFacilityIdsAsync(requestingFacilityId.Value);
                if (ancestors.Contains(broadcast.FacilityId))
                {
                    return ApiResponse<BroadcastDetailResponseDto>.Fail("Bản tin do cơ sở cha tạo chỉ cho phép xem chi tiết, không được phép thao tác (USR-BRD-01)!");
                }
            }

            // KIỂM TRA QUY TẮC: "Bản tin ưu tiên: không áp dụng dừng phát/tiếp tục phát"
            if (broadcast.Priority == 1 || broadcast.BroadcastType == "Ưu tiên" || broadcast.BroadcastType == "Khẩn cấp")
            {
                return ApiResponse<BroadcastDetailResponseDto>.Fail("Bản tin ưu tiên không áp dụng tính năng tiếp tục phát!");
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

            if (_realtimeDispatcher != null)
            {
                try
                {
                    await _realtimeDispatcher.DispatchBroadcastToDevicesAsync(broadcast.Id);
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Lỗi realtime dispatch khi resume bản tin {BroadcastId}", broadcast.Id);
                }
            }

            return await GetByIdAsync(broadcast.Id, requestingFacilityId);
        }

        // =========================================================
        // USR-BRD-11: BẢN TIN CƠ SỞ CON (Xem danh sách bản tin của cơ sở con)
        // =========================================================
        public async Task<ApiResponse<PagedResult<BroadcastListResponseDto>>> GetSubFacilityBroadcastsAsync(
            Guid parentFacilityId,
            string? search,
            string? status = null,
            string? broadcastType = null,
            int pageIndex = 1,
            int pageSize = 10,
            Guid? subFacilityId = null,
            Guid? deviceId = null,
            DateTimeOffset? startDateFrom = null,
            DateTimeOffset? startDateTo = null,
            string? broadcastForm = null)
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

            if (subFacilityId.HasValue && subFacilityId.Value != Guid.Empty && subFacilityIds.Contains(subFacilityId.Value))
            {
                subFacilityIds = new List<Guid> { subFacilityId.Value };
            }

            var query = _context.Broadcasts
                .AsNoTracking()
                .Where(b => !b.IsDeleted && subFacilityIds.Contains(b.FacilityId));

            if (!string.IsNullOrWhiteSpace(status) && status.Trim() != "Tất cả")
            {
                query = query.Where(b => b.Status == status.Trim());
            }

            if (!string.IsNullOrWhiteSpace(broadcastType) && broadcastType.Trim() != "Tất cả")
            {
                var typeTrim = broadcastType.Trim();
                if (typeTrim.Equals("Tin thường", StringComparison.OrdinalIgnoreCase) || typeTrim.Equals("Thường", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(b => b.BroadcastType == "Thường" && b.Priority != 1);
                }
                else if (typeTrim.Equals("Tin ưu tiên", StringComparison.OrdinalIgnoreCase) || typeTrim.Equals("Ưu tiên", StringComparison.OrdinalIgnoreCase) || typeTrim.Equals("Khẩn cấp", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(b => b.BroadcastType == "Khẩn cấp" || b.BroadcastType == "Ưu tiên" || b.Priority == 1);
                }
                else
                {
                    query = query.Where(b => b.BroadcastType == typeTrim);
                }
            }

            if (deviceId.HasValue && deviceId.Value != Guid.Empty)
            {
                var targetDevId = deviceId.Value;
                query = query.Where(b => b.Schedules.Any(s => !s.IsDeleted && s.BroadcastDevices.Any(bd => !bd.IsDeleted && bd.DeviceId == targetDevId)));
            }

            if (startDateFrom.HasValue)
            {
                var fromDate = new DateTimeOffset(startDateFrom.Value.UtcDateTime.Date, TimeSpan.Zero);
                query = query.Where(b => b.Schedules.Any(s => !s.IsDeleted && s.StartDate >= fromDate));
            }

            if (startDateTo.HasValue)
            {
                var toDate = new DateTimeOffset(startDateTo.Value.UtcDateTime.Date.AddDays(1), TimeSpan.Zero);
                query = query.Where(b => b.Schedules.Any(s => !s.IsDeleted && s.StartDate < toDate));
            }

            if (!string.IsNullOrWhiteSpace(broadcastForm) && broadcastForm.Trim() != "Tất cả")
            {
                var formTrim = broadcastForm.Trim();
                if (formTrim.Equals("Bản tin nhanh", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(b => b.Description != null && b.Description.Contains(TAG_PLAY_MODE_IMMEDIATE));
                }
                else if (formTrim.Equals("Bản tin tiêu chuẩn", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(b => b.Description == null || !b.Description.Contains(TAG_PLAY_MODE_IMMEDIATE));
                }
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                query = query.Where(b => b.BroadcastCode.ToLower().Contains(term) || b.BroadcastName.ToLower().Contains(term) || (b.Description != null && b.Description.ToLower().Contains(term)));
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
                    b.CreatedBy,
                    b.CreatedAt,
                    TotalSchedules = b.Schedules.Count(s => !s.IsDeleted),
                    FirstSchedule = b.Schedules
                        .Where(s => !s.IsDeleted)
                        .OrderBy(s => s.StartDate)
                        .Select(s => new
                        {
                            s.StartDate,
                            s.EndDate,
                            TimeSlots = s.TimeSlots
                                .Where(ts => !ts.IsDeleted)
                                .OrderBy(ts => ts.StartTime)
                                .Select(ts => new
                                {
                                    ts.StartTime,
                                    ts.EndTime
                                })
                                .ToList()
                        })
                        .FirstOrDefault()
                })
                .ToListAsync();

            var facilityMap = await _context.Facilities
                .AsNoTracking()
                .Where(f => subFacilityIds.Contains(f.Id))
                .ToDictionaryAsync(f => f.Id, f => f.FacilityName);

            var creatorIds = broadcasts
                .Where(b => b.CreatedBy.HasValue)
                .Select(b => b.CreatedBy!.Value)
                .Distinct()
                .ToList();

            var userMap = new Dictionary<Guid, string>();
            if (creatorIds.Any())
            {
                userMap = await _context.Users
                    .AsNoTracking()
                    .Where(u => creatorIds.Contains(u.Id))
                    .ToDictionaryAsync(u => u.Id, u => u.FullName);
            }

            var items = broadcasts.Select(b =>
            {
                facilityMap.TryGetValue(b.FacilityId, out var fName);
                bool isPriority = b.Priority == 1 || b.BroadcastType == "Ưu tiên" || b.BroadcastType == "Khẩn cấp";

                var isImmediate = (b.Description != null && b.Description.Contains(TAG_PLAY_MODE_IMMEDIATE));
                var playMode = isImmediate ? PLAY_MODE_IMMEDIATE : PLAY_MODE_SCHEDULED;
                var broadcastForm = isImmediate ? "Bản tin nhanh" : "Bản tin tiêu chuẩn";
                var broadcastTypeDisplay = isPriority ? "Tin ưu tiên" : "Tin thường";
                var cleanDesc = b.Description?.Replace(TAG_PLAY_MODE_IMMEDIATE, "").Trim();

                string broadcastDates = "-";
                string broadcastTimes = "-";
                if (b.FirstSchedule != null)
                {
                    broadcastDates = $"{b.FirstSchedule.StartDate:dd/MM/yyyy} - {b.FirstSchedule.EndDate:dd/MM/yyyy}";
                    if (b.FirstSchedule.TimeSlots != null && b.FirstSchedule.TimeSlots.Any())
                    {
                        broadcastTimes = string.Join(", ", b.FirstSchedule.TimeSlots.Select(ts => $"{ts.StartTime:hh\\:mm\\:ss} - {ts.EndTime:hh\\:mm\\:ss}"));
                    }
                }

                string createdByName = "Nguyễn Văn A";
                if (b.CreatedBy.HasValue && userMap.TryGetValue(b.CreatedBy.Value, out var uName) && !string.IsNullOrWhiteSpace(uName))
                {
                    createdByName = uName;
                }

                return new BroadcastListResponseDto
                {
                    Id = b.Id,
                    BroadcastCode = b.BroadcastCode,
                    BroadcastName = b.BroadcastName,
                    Description = cleanDesc,
                    BroadcastType = b.BroadcastType,
                    Priority = b.Priority,
                    Status = b.Status,
                    PlayMode = playMode,
                    ContentType = b.ContentType,
                    OrganizationId = b.OrganizationId,
                    FacilityId = b.FacilityId,
                    FacilityName = fName,
                    ApprovedAt = b.ApprovedAt,
                    ApprovedBy = b.ApprovedBy,
                    CreatedBy = b.CreatedBy,
                    CreatedByName = createdByName,
                    CreatedAt = b.CreatedAt,
                    TotalSchedules = b.TotalSchedules,
                    BroadcastForm = broadcastForm,
                    BroadcastTypeDisplay = broadcastTypeDisplay,
                    BroadcastDates = broadcastDates,
                    BroadcastTimes = broadcastTimes,
                    CanEdit = false,
                    CanDelete = b.Status == "Chờ duyệt" || b.Status == "Từ chối duyệt" || b.Status == "Đã hủy",
                    CanApprove = b.Status == "Chờ duyệt",
                    CanReject = b.Status == "Chờ duyệt",
                    CanCancel = b.Status == "Chưa phát" || b.Status == "Sắp tới lịch" || b.Status == "Đang phát" || b.Status == "Chờ phát" || b.Status == "Tạm ngưng" || b.Status == "Đã duyệt",
                    CanPause = b.Status == "Đang phát" && !isPriority,
                    CanResume = b.Status == "Tạm ngưng" && !isPriority,
                    IsFromParent = false
                };
            }).ToList();

            var pagedResult = PagedResult<BroadcastListResponseDto>.Create(items, totalCount, pageIndex, pageSize);
            return ApiResponse<PagedResult<BroadcastListResponseDto>>.Ok(pagedResult);
        }

        // =========================================================
        // USR-BRD-11: XEM CHI TIẾT BẢN TIN CỦA CƠ SỞ CON
        // =========================================================
        public async Task<ApiResponse<BroadcastDetailResponseDto>> GetSubFacilityBroadcastDetailAsync(
            Guid parentFacilityId,
            Guid broadcastId)
        {
            var subFacilityIds = await GetAllSubFacilityIdsAsync(parentFacilityId);
            var broadcast = await _context.Broadcasts
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.Id == broadcastId && !b.IsDeleted);

            if (broadcast == null)
            {
                return ApiResponse<BroadcastDetailResponseDto>.Fail("Không tìm thấy bản tin!");
            }

            if (!subFacilityIds.Contains(broadcast.FacilityId))
            {
                return ApiResponse<BroadcastDetailResponseDto>.Fail("Bản tin không thuộc cơ sở con trực thuộc của bạn!");
            }

            return await GetByIdAsync(broadcastId, parentFacilityId);
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
                .Include(s => s.BroadcastDevices.Where(bd => !bd.IsDeleted))
                .ToListAsync();

            var broadcast = await _context.Broadcasts.AsNoTracking().FirstOrDefaultAsync(b => b.Id == broadcastId);
            int playlistDuration = 0;
            if (broadcast?.PlaylistId.HasValue == true)
            {
                var pl = await _context.Playlists.AsNoTracking().FirstOrDefaultAsync(p => p.Id == broadcast.PlaylistId.Value);
                playlistDuration = pl?.TotalDurationSeconds ?? 0;
            }

            var deviceIds = schedules.SelectMany(s => s.BroadcastDevices).Select(bd => bd.DeviceId).Distinct().ToList();
            var devicesMap = await _context.Devices
                .AsNoTracking()
                .Where(d => deviceIds.Contains(d.Id))
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
                TimeSlots = s.TimeSlots.Select(t =>
                {
                    int repCount = 1;
                    if (playlistDuration > 0 && t.DurationSeconds > 0)
                    {
                        repCount = Math.Max(1, (int)Math.Round((double)t.DurationSeconds / playlistDuration));
                    }
                    return new BroadcastTimeSlotResponseDto
                    {
                        Id = t.Id,
                        BroadcastScheduleId = t.BroadcastScheduleId,
                        StartTime = t.StartTime,
                        EndTime = t.EndTime,
                        DurationSeconds = t.DurationSeconds,
                        RepeatCount = repCount
                    };
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

            bool isPriority = broadcast.Priority == 1 || broadcast.BroadcastType == "Ưu tiên" || broadcast.BroadcastType == "Khẩn cấp";

            if (dto.EndDate < dto.StartDate)
            {
                return ApiResponse<BroadcastScheduleResponseDto>.Fail("Ngày kết thúc phát phải lớn hơn hoặc bằng ngày bắt đầu!");
            }

            // KIỂM TRA QUY TẮC: Bản tin thường không được phép phát qua ngày
            if (!isPriority)
            {
                if (dto.StartDate.Date != dto.EndDate.Date)
                {
                    return ApiResponse<BroadcastScheduleResponseDto>.Fail(
                        $"Bản tin thường không được phép phát qua ngày (Từ {dto.StartDate:yyyy-MM-dd} đến {dto.EndDate:yyyy-MM-dd}). Chỉ bản tin ưu tiên mới được phép phát qua ngày!");
                }

                if (dto.TimeSlots != null)
                {
                    foreach (var slot in dto.TimeSlots)
                    {
                        if (slot.EndTime <= slot.StartTime)
                        {
                            return ApiResponse<BroadcastScheduleResponseDto>.Fail(
                                $"Bản tin thường không được cấu hình khung giờ phát qua đêm hoặc giờ kết thúc nhỏ hơn/bằng giờ bắt đầu ({slot.StartTime} - {slot.EndTime})!");
                        }
                    }
                }
            }

            int playlistDuration = 0;
            if (broadcast.ContentType == "Playlist" && broadcast.PlaylistId.HasValue)
            {
                var pl = await _context.Playlists.AsNoTracking().FirstOrDefaultAsync(p => p.Id == broadcast.PlaylistId.Value);
                playlistDuration = pl?.TotalDurationSeconds ?? 0;
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
                foreach (var ts in dto.TimeSlots)
                {
                    int repCount = ts.RepeatCount > 0 ? ts.RepeatCount : 1;
                    int duration;
                    if (broadcast.ContentType == "Playlist" && playlistDuration > 0)
                    {
                        duration = playlistDuration * repCount;
                    }
                    else
                    {
                        duration = ts.DurationSeconds > 0
                            ? ts.DurationSeconds * repCount
                            : (int)(ts.EndTime - ts.StartTime).TotalSeconds * repCount;
                    }

                    schedule.TimeSlots.Add(new BroadcastTimeSlot
                    {
                        BroadcastScheduleId = schedule.Id,
                        StartTime = ts.StartTime,
                        EndTime = ts.EndTime,
                        DurationSeconds = Math.Max(0, duration),
                        CreatedAt = DateTimeOffset.UtcNow
                    });
                }
            }

            if (dto.DeviceIds != null)
            {
                foreach (var devId in dto.DeviceIds.Distinct())
                {
                    schedule.BroadcastDevices.Add(new BroadcastDevice
                    {
                        BroadcastScheduleId = schedule.Id,
                        DeviceId = devId,
                        DeviceStatus = "Chờ phát",
                        CreatedAt = DateTimeOffset.UtcNow
                    });
                }
            }

            _context.BroadcastSchedules.Add(schedule);
            await _context.SaveChangesAsync();

            var schedulesRes = await GetSchedulesAsync(broadcastId);
            var created = schedulesRes.Data?.FirstOrDefault(s => s.Id == schedule.Id);
            return ApiResponse<BroadcastScheduleResponseDto>.Ok(created!, "Thêm lịch phát thành công!");
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
                return ApiResponse<BroadcastScheduleResponseDto>.Fail("Lịch phát đã bị khóa (bản tin đang phát), không thể chỉnh sửa!");
            }

            var broadcast = await _context.Broadcasts.AsNoTracking().FirstOrDefaultAsync(b => b.Id == schedule.BroadcastId);
            bool isPriority = broadcast != null && (broadcast.Priority == 1 || broadcast.BroadcastType == "Ưu tiên" || broadcast.BroadcastType == "Khẩn cấp");

            if (dto.EndDate < dto.StartDate)
            {
                return ApiResponse<BroadcastScheduleResponseDto>.Fail("Ngày kết thúc phát phải lớn hơn hoặc bằng ngày bắt đầu!");
            }

            if (!isPriority)
            {
                if (dto.StartDate.Date != dto.EndDate.Date)
                {
                    return ApiResponse<BroadcastScheduleResponseDto>.Fail(
                        $"Bản tin thường không được phép phát qua ngày (Từ {dto.StartDate:yyyy-MM-dd} đến {dto.EndDate:yyyy-MM-dd}). Chỉ bản tin ưu tiên mới được phép phát qua ngày!");
                }

                if (dto.TimeSlots != null)
                {
                    foreach (var slot in dto.TimeSlots)
                    {
                        if (slot.EndTime <= slot.StartTime)
                        {
                            return ApiResponse<BroadcastScheduleResponseDto>.Fail(
                                $"Bản tin thường không được cấu hình khung giờ phát qua đêm hoặc giờ kết thúc nhỏ hơn/bằng giờ bắt đầu ({slot.StartTime} - {slot.EndTime})!");
                        }
                    }
                }
            }

            int playlistDuration = 0;
            if (broadcast?.ContentType == "Playlist" && broadcast.PlaylistId.HasValue)
            {
                var pl = await _context.Playlists.AsNoTracking().FirstOrDefaultAsync(p => p.Id == broadcast.PlaylistId.Value);
                playlistDuration = pl?.TotalDurationSeconds ?? 0;
            }

            schedule.StartDate = dto.StartDate;
            schedule.EndDate = dto.EndDate;
            schedule.RepeatType = string.IsNullOrWhiteSpace(dto.RepeatType) ? "Không lặp" : dto.RepeatType.Trim();
            schedule.RepeatDays = dto.RepeatDays;
            schedule.UpdatedAt = DateTimeOffset.UtcNow;

            var now = DateTimeOffset.UtcNow;
            foreach (var ts in schedule.TimeSlots)
            {
                ts.IsDeleted = true;
                ts.UpdatedAt = now;
            }

            if (dto.TimeSlots != null)
            {
                foreach (var ts in dto.TimeSlots)
                {
                    int repCount = ts.RepeatCount > 0 ? ts.RepeatCount : 1;
                    int duration;
                    if (broadcast?.ContentType == "Playlist" && playlistDuration > 0)
                    {
                        duration = playlistDuration * repCount;
                    }
                    else
                    {
                        duration = ts.DurationSeconds > 0
                            ? ts.DurationSeconds * repCount
                            : (int)(ts.EndTime - ts.StartTime).TotalSeconds * repCount;
                    }

                    schedule.TimeSlots.Add(new BroadcastTimeSlot
                    {
                        BroadcastScheduleId = schedule.Id,
                        StartTime = ts.StartTime,
                        EndTime = ts.EndTime,
                        DurationSeconds = Math.Max(0, duration),
                        CreatedAt = now
                    });
                }
            }

            foreach (var bd in schedule.BroadcastDevices)
            {
                bd.IsDeleted = true;
                bd.UpdatedAt = now;
            }

            if (dto.DeviceIds != null)
            {
                foreach (var devId in dto.DeviceIds.Distinct())
                {
                    schedule.BroadcastDevices.Add(new BroadcastDevice
                    {
                        BroadcastScheduleId = schedule.Id,
                        DeviceId = devId,
                        DeviceStatus = "Chờ phát",
                        CreatedAt = now
                    });
                }
            }

            await _context.SaveChangesAsync();

            var schedulesRes = await GetSchedulesAsync(schedule.BroadcastId);
            var updated = schedulesRes.Data?.FirstOrDefault(s => s.Id == schedule.Id);
            return ApiResponse<BroadcastScheduleResponseDto>.Ok(updated!, "Cập nhật lịch phát thành công!");
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
                return ApiResponse<bool>.Fail("Lịch phát đã bị khóa (bản tin đang phát), không thể xóa!");
            }

            var now = DateTimeOffset.UtcNow;
            schedule.IsDeleted = true;
            schedule.UpdatedAt = now;

            foreach (var ts in schedule.TimeSlots)
            {
                ts.IsDeleted = true;
                ts.UpdatedAt = now;
            }

            foreach (var bd in schedule.BroadcastDevices)
            {
                bd.IsDeleted = true;
                bd.UpdatedAt = now;
            }

            await _context.SaveChangesAsync();
            return ApiResponse<bool>.Ok(true, "Xóa lịch phát thành công!");
        }

        // =========================================================
        // THUẬT TOÁN QUÉT TRÙNG LỊCH (CONFLICT ENGINE - ISSUE #14, #15)
        // Quét giao cắt thời gian giữa các khung giờ và chu kỳ lặp
        // =========================================================
        private async Task<ConflictCheckResultDto> CheckConflictEngineAsync(
            Guid organizationId,
            Guid facilityId,
            List<Guid> targetDeviceIds,
            DateTimeOffset startDate,
            DateTimeOffset endDate,
            string? repeatType,
            string? repeatDays,
            List<CreateBroadcastTimeSlotDto> newTimeSlots,
            Guid? excludeBroadcastId,
            Guid? excludeScheduleId)
        {
            var result = new ConflictCheckResultDto();
            var broadcastMode = await GetBroadcastModeAsync(organizationId);
            result.BroadcastMode = broadcastMode;

            var facility = await _context.Facilities.AsNoTracking().FirstOrDefaultAsync(f => f.Id == facilityId);
            result.FacilityName = facility?.FacilityName;

            if (!targetDeviceIds.Any() || !newTimeSlots.Any())
            {
                result.SummaryMessage = "Không có thiết bị hoặc khung giờ nào được chọn để quét trùng lịch!";
                return result;
            }

            var activeStatuses = new[] { "Chưa phát", "Sắp tới lịch", "Đang phát", "Chờ phát", "Tạm ngưng", "Đã duyệt" };

            // Tìm các lịch phát trong CSDL có khoảng ngày giao nhau: (sch.StartDate.Date <= endDate.Date && sch.EndDate.Date >= startDate.Date)
            var existingSchedulesQuery = _context.BroadcastSchedules
                .AsNoTracking()
                .Where(s => !s.IsDeleted &&
                            s.BroadcastId != (excludeBroadcastId ?? Guid.Empty) &&
                            (!excludeScheduleId.HasValue || s.Id != excludeScheduleId.Value) &&
                            s.StartDate.Date <= endDate.Date &&
                            s.EndDate.Date >= startDate.Date)
                .Join(_context.Broadcasts.Where(b => !b.IsDeleted &&
                                                     b.OrganizationId == organizationId &&
                                                     activeStatuses.Contains(b.Status)),
                      s => s.BroadcastId,
                      b => b.Id,
                      (s, b) => new { Schedule = s, Broadcast = b });

            var existingSchedules = await existingSchedulesQuery
                .Select(x => new
                {
                    x.Schedule.Id,
                    x.Schedule.BroadcastId,
                    x.Broadcast.BroadcastCode,
                    x.Broadcast.BroadcastName,
                    x.Broadcast.BroadcastType,
                    x.Broadcast.Priority,
                    x.Broadcast.FacilityId,
                    x.Schedule.StartDate,
                    x.Schedule.EndDate,
                    x.Schedule.RepeatType,
                    x.Schedule.RepeatDays,
                    TimeSlots = x.Schedule.TimeSlots.Where(t => !t.IsDeleted).Select(t => new { t.StartTime, t.EndTime }).ToList(),
                    DeviceIds = x.Schedule.BroadcastDevices.Where(d => !d.IsDeleted && !d.IsPreempted).Select(d => d.DeviceId).ToList()
                })
                .ToListAsync();

            if (!existingSchedules.Any())
            {
                result.HasConflict = false;
                result.SummaryMessage = "Không phát hiện xung đột lịch phát nào. Lịch phát sóng sẵn sàng!";
                return result;
            }

            var devicesMap = await _context.Devices
                .AsNoTracking()
                .Where(d => targetDeviceIds.Contains(d.Id))
                .ToDictionaryAsync(d => d.Id, d => (d.DeviceName, d.DeviceCode));

            var conflictingItems = new List<ConflictingBroadcastItemDto>();

            foreach (var sch in existingSchedules)
            {
                // Nếu chế độ phát theo cơ sở: nếu schedule cũ không gán thiết bị cụ thể thì xem như phát toàn cơ sở
                List<Guid> overlappingDevices;
                if (broadcastMode == "Phát theo cơ sở")
                {
                    overlappingDevices = sch.DeviceIds.Any()
                        ? targetDeviceIds.Intersect(sch.DeviceIds).ToList()
                        : targetDeviceIds.ToList();
                }
                else
                {
                    overlappingDevices = targetDeviceIds.Intersect(sch.DeviceIds).ToList();
                }

                if (!overlappingDevices.Any()) continue;

                // QUÉT GIAO CẮT CHU KỲ LẶP (REPEAT RULE INTERSECTION)
                var rangeStart = startDate.Date > sch.StartDate.Date ? startDate.Date : sch.StartDate.Date;
                var rangeEnd = endDate.Date < sch.EndDate.Date ? endDate.Date : sch.EndDate.Date;

                bool hasIntersection = false;
                DateTimeOffset? nearestConflictTime = null;
                int nearestConflictDurationMinutes = 0;
                TimeSpan matchedSlotStartTime = TimeSpan.Zero;
                TimeSpan matchedSlotEndTime = TimeSpan.Zero;

                for (var d = rangeStart; d <= rangeEnd; d = d.AddDays(1))
                {
                    var currentDateOffset = new DateTimeOffset(d, startDate.Offset);

                    // Kiểm tra xem ngày d có thỏa mãn đồng thời chu kỳ lặp của cả 2 lịch
                    if (IsDateMatchingRepeatRule(repeatType, repeatDays, currentDateOffset) &&
                        IsDateMatchingRepeatRule(sch.RepeatType, sch.RepeatDays, currentDateOffset))
                    {
                        foreach (var newSlot in newTimeSlots)
                        {
                            foreach (var existSlot in sch.TimeSlots)
                            {
                                // Công thức giao cắt khung giờ: (NewStart < ExistEnd) && (NewEnd > ExistStart)
                                if (newSlot.StartTime < existSlot.EndTime && newSlot.EndTime > existSlot.StartTime)
                                {
                                    hasIntersection = true;
                                    var conflictStart = newSlot.StartTime > existSlot.StartTime ? newSlot.StartTime : existSlot.StartTime;
                                    var conflictEnd = newSlot.EndTime < existSlot.EndTime ? newSlot.EndTime : existSlot.EndTime;
                                    var conflictDuration = (int)(conflictEnd - conflictStart).TotalMinutes;
                                    var conflictTime = new DateTimeOffset(d.Add(conflictStart), startDate.Offset);

                                    if (!nearestConflictTime.HasValue || conflictTime < nearestConflictTime.Value)
                                    {
                                        nearestConflictTime = conflictTime;
                                        nearestConflictDurationMinutes = Math.Max(1, conflictDuration);
                                        matchedSlotStartTime = existSlot.StartTime;
                                        matchedSlotEndTime = existSlot.EndTime;
                                    }
                                }
                            }
                        }
                    }
                }

                if (hasIntersection && nearestConflictTime.HasValue)
                {
                    foreach (var devId in overlappingDevices)
                    {
                        devicesMap.TryGetValue(devId, out var devInfo);

                        conflictingItems.Add(new ConflictingBroadcastItemDto
                        {
                            BroadcastId = sch.BroadcastId,
                            BroadcastCode = sch.BroadcastCode,
                            BroadcastName = sch.BroadcastName,
                            BroadcastType = sch.BroadcastType,
                            Priority = sch.Priority,
                            DeviceId = devId,
                            DeviceName = devInfo.DeviceName,
                            DeviceCode = devInfo.DeviceCode,
                            FacilityName = facility?.FacilityName,
                            StartTime = matchedSlotStartTime,
                            EndTime = matchedSlotEndTime,
                            ConflictStartDate = rangeStart,
                            ConflictEndDate = rangeEnd,
                            ConflictDurationMinutes = nearestConflictDurationMinutes,
                            NearestConflictTime = nearestConflictTime,
                            NearestConflictDurationMinutes = nearestConflictDurationMinutes
                        });
                    }
                }
            }

            if (!conflictingItems.Any())
            {
                result.HasConflict = false;
                result.SummaryMessage = "Không phát hiện xung đột lịch phát nào. Lịch phát sóng sẵn sàng!";
                return result;
            }

            result.HasConflict = true;
            result.ConflictingBroadcasts = conflictingItems;

            var distinctBroadcastNames = conflictingItems.Select(c => c.BroadcastName).Distinct().ToList();
            result.TotalConflictingBroadcasts = distinctBroadcastNames.Count;

            // Xây dựng chuỗi thông báo theo yêu cầu:
            // 1. Chế độ Theo cơ sở: trả về tên cơ sở chứa thiết bị trùng lịch (ví dụ cơ sở có 4 thiết bị phát 4 bản tin khác nhau thì báo trùng cả 4 bản tin)
            if (broadcastMode == "Phát theo cơ sở")
            {
                var distinctDeviceCount = conflictingItems.Select(c => c.DeviceId).Distinct().Count();
                result.SummaryMessage = $"Cơ sở '{facility?.FacilityName ?? "Chỉ định"}' có {distinctDeviceCount} thiết bị trùng lịch phát với {distinctBroadcastNames.Count} bản tin: " +
                                        $"{string.Join(", ", distinctBroadcastNames.Select(n => $"'{n}'"))}. " +
                                        $"Vui lòng kiểm tra lại khung giờ hoặc sử dụng nút [Ghi đè]!";
            }
            // 2. Chế độ Theo thiết bị: trả về tên thiết bị có lịch trùng, kèm thời điểm và thời lượng trùng gần nhất
            else
            {
                var groupedByDevice = conflictingItems
                    .GroupBy(c => new { c.DeviceId, c.DeviceName, c.DeviceCode })
                    .Select(g =>
                    {
                        var earliest = g.OrderBy(x => x.NearestConflictTime).First();
                        var bNames = string.Join(", ", g.Select(x => $"'{x.BroadcastName}'").Distinct());
                        return $"Thiết bị '{g.Key.DeviceName ?? g.Key.DeviceCode}' trùng với {g.Select(x => x.BroadcastId).Distinct().Count()} bản tin ({bNames}) lúc {earliest.NearestConflictTime:dd/MM/yyyy HH:mm} (thời lượng trùng: {earliest.NearestConflictDurationMinutes} phút)";
                    })
                    .ToList();

                result.SummaryMessage = $"Phát hiện trùng lịch trên các thiết bị: {string.Join("; ", groupedByDevice)}. " +
                                        $"Vui lòng kiểm tra lại khung giờ hoặc sử dụng nút [Ghi đè]!";
            }

            return result;
        }

        /// <summary>
        /// Kiểm tra 4 kiểu chu kỳ lặp:
        /// 1. "Không lặp": chỉ phát trong khoảng StartDate - EndDate
        /// 2. "Lặp hàng ngày" / "Hàng ngày": lặp tất cả các ngày
        /// 3. "Lặp theo tuần" / "Theo tuần" vào các thứ (2,3,4,5,6,7,CN)
        /// 4. "Lặp theo tháng" / "Theo tháng" vào các ngày cụ thể (1,15,28)
        /// </summary>
        public static bool IsDateMatchingRepeatRule(string? repeatType, string? repeatDays, DateTimeOffset date)
        {
            var type = repeatType?.Trim() ?? "Không lặp";

            if (type.Equals("Không lặp", StringComparison.OrdinalIgnoreCase))
                return true;

            if (type.Equals("Hàng ngày", StringComparison.OrdinalIgnoreCase) ||
                type.Equals("Lặp hàng ngày", StringComparison.OrdinalIgnoreCase))
                return true;

            if (type.Equals("Theo tuần", StringComparison.OrdinalIgnoreCase) ||
                type.Equals("Lặp theo tuần", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(repeatDays))
                    return true;

                var dayOfWeek = date.DayOfWeek;
                var vnDayCode = dayOfWeek switch
                {
                    DayOfWeek.Monday => "2",
                    DayOfWeek.Tuesday => "3",
                    DayOfWeek.Wednesday => "4",
                    DayOfWeek.Thursday => "5",
                    DayOfWeek.Friday => "6",
                    DayOfWeek.Saturday => "7",
                    DayOfWeek.Sunday => "CN",
                    _ => ""
                };

                var days = repeatDays.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                return days.Contains(vnDayCode, StringComparer.OrdinalIgnoreCase) ||
                       days.Contains(dayOfWeek.ToString(), StringComparer.OrdinalIgnoreCase);
            }

            if (type.Equals("Theo tháng", StringComparison.OrdinalIgnoreCase) ||
                type.Equals("Lặp theo tháng", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(repeatDays))
                    return true;

                var dayOfMonth = date.Day.ToString();
                var days = repeatDays.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                return days.Contains(dayOfMonth);
            }

            return true;
        }

        private async Task<string> GetBroadcastModeAsync(Guid organizationId)
        {
            var setting = await _context.SystemSettings
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.OrganizationId == organizationId && s.SettingKey == KEY_BROADCAST_MODE && !s.IsDeleted);

            return setting?.SettingValue ?? "Phát theo cơ sở";
        }

        /// <summary>
        /// Kiểm tra xem cơ sở cha có đang ghi đè lên các bản tin của cơ sở con hay không
        /// </summary>
        private async Task<bool> IsParentOverridingChildBroadcastsAsync(Guid parentFacilityId, List<Guid> conflictingBroadcastIds)
        {
            if (!conflictingBroadcastIds.Any()) return false;

            var subFacilityIds = await GetAllSubFacilityIdsAsync(parentFacilityId);
            if (!subFacilityIds.Any()) return false;

            var conflictingFacilityIds = await _context.Broadcasts
                .AsNoTracking()
                .Where(b => conflictingBroadcastIds.Contains(b.Id) && !b.IsDeleted)
                .Select(b => b.FacilityId)
                .Distinct()
                .ToListAsync();

            // Nếu tất cả các bản tin bị đè đều thuộc các cơ sở con trực thuộc/gián tiếp của parentFacilityId
            return conflictingFacilityIds.All(fid => subFacilityIds.Contains(fid));
        }

        /// <summary>
        /// Xử lý ghi đè và gửi thông báo NTF-09 cho người tạo bản tin bị đè
        /// </summary>
        private async Task PreemptAndNotifyOverwrittenBroadcastsAsync(Broadcast currentBroadcast, List<Guid> conflictingBroadcastIds)
        {
            if (!conflictingBroadcastIds.Any()) return;

            var targetDeviceIds = currentBroadcast.Schedules
                .SelectMany(s => s.BroadcastDevices)
                .Select(bd => bd.DeviceId)
                .Distinct()
                .ToList();

            if (!targetDeviceIds.Any())
            {
                targetDeviceIds = await _context.Devices
                    .AsNoTracking()
                    .Where(d => d.FacilityId == currentBroadcast.FacilityId && !d.IsDeleted)
                    .Select(d => d.Id)
                    .ToListAsync();
            }

            var conflictingDevices = await _context.BroadcastDevices
                .Where(bd => !bd.IsDeleted &&
                             targetDeviceIds.Contains(bd.DeviceId) &&
                             !bd.IsPreempted &&
                             _context.BroadcastSchedules.Any(s => s.Id == bd.BroadcastScheduleId &&
                                                                 conflictingBroadcastIds.Contains(s.BroadcastId) &&
                                                                 !s.IsDeleted))
                .ToListAsync();

            var now = DateTimeOffset.UtcNow;
            foreach (var bd in conflictingDevices)
            {
                bd.IsPreempted = true;
                bd.PreemptedByBroadcastId = currentBroadcast.Id;
                bd.DeviceStatus = "Tạm ngưng";
                bd.UpdatedAt = now;
            }

            // GỬI THÔNG BÁO NTF-09 CHO NGƯỜI TẠO BẢN TIN BỊ ĐÈ
            var overwrittenBroadcasts = await _context.Broadcasts
                .Where(b => conflictingBroadcastIds.Contains(b.Id) && !b.IsDeleted)
                .ToListAsync();

            foreach (var ob in overwrittenBroadcasts)
            {
                if (ob.CreatedBy.HasValue)
                {
                    var notification = new SystemNotification
                    {
                        Id = Guid.NewGuid(),
                        OrganizationId = ob.OrganizationId,
                        FacilityId = ob.FacilityId,
                        Title = "Bản tin bị ghi đè phát sóng",
                        Content = $"Bản tin '{ob.BroadcastName}' ({ob.BroadcastCode}) của bạn đã bị ghi đè phát sóng bởi bản tin '{currentBroadcast.BroadcastName}' ({currentBroadcast.BroadcastCode}).",
                        NotificationType = "NTF-09",
                        TargetUserId = ob.CreatedBy.Value,
                        IsSent = true,
                        SentAt = now,
                        CreatedAt = now
                    };
                    _context.SystemNotifications.Add(notification);
                }
            }

            // Gửi lệnh ngắt phát tới các thiết bị đang online nếu có SignalR connection
            if (_connectionManager != null && _hubContext != null && _commandManager != null)
            {
                foreach (var devId in targetDeviceIds)
                {
                    if (_connectionManager.TryGetConnectionId(devId, out var connId) && !string.IsNullOrEmpty(connId))
                    {
                        try
                        {
                            var cmdId = Guid.NewGuid();
                            _commandManager.RegisterCommand(cmdId, devId, "StopBroadcast", "Pending");
                            await _hubContext.Clients.Client(connId).SendAsync("ReceiveCommand", new DeviceCommandEnvelope
                            {
                                CommandId = cmdId,
                                DeviceId = devId,
                                Type = "StopBroadcast",
                                Payload = new
                                {
                                    reason = "PreemptedByOverwrite",
                                    priorityBroadcastId = currentBroadcast.Id,
                                    stoppedAt = now
                                }
                            });
                            _commandManager.MarkSent(cmdId);
                        }
                        catch (Exception ex)
                        {
                            _logger?.LogWarning(ex, "Lỗi gửi lệnh dừng phát ngắt sóng tới thiết bị {DeviceId}", devId);
                        }
                    }
                }
            }

            await _context.SaveChangesAsync();
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

            var currentBroadcast = await _context.Broadcasts.FirstOrDefaultAsync(b => b.Id == currentBroadcastId && !b.IsDeleted);
            if (currentBroadcast == null) return;

            var targetDeviceIds = currentSchedules.SelectMany(s => s.BroadcastDevices).Select(bd => bd.DeviceId).Distinct().ToList();

            if (!targetDeviceIds.Any())
            {
                targetDeviceIds = await _context.Devices
                    .AsNoTracking()
                    .Where(d => d.FacilityId == currentBroadcast.FacilityId && !d.IsDeleted)
                    .Select(d => d.Id)
                    .ToListAsync();
            }

            var activeStatuses = new[] { "Chưa phát", "Sắp tới lịch", "Đang phát", "Chờ phát", "Tạm ngưng", "Đã duyệt" };
            var conflictingScheduleIds = await _context.BroadcastSchedules
                .Where(s => s.BroadcastId != currentBroadcastId && !s.IsDeleted &&
                            _context.Broadcasts.Any(b => b.Id == s.BroadcastId && !b.IsDeleted && activeStatuses.Contains(b.Status)))
                .Select(s => s.Id)
                .ToListAsync();

            var conflictingDevices = await _context.BroadcastDevices
                .Where(bd => !bd.IsDeleted &&
                             targetDeviceIds.Contains(bd.DeviceId) &&
                             !bd.IsPreempted &&
                             conflictingScheduleIds.Contains(bd.BroadcastScheduleId))
                .ToListAsync();

            var scheduleDict = await _context.BroadcastSchedules
                .AsNoTracking()
                .Where(s => conflictingScheduleIds.Contains(s.Id))
                .ToDictionaryAsync(s => s.Id, s => (s.StartDate, s.EndDate));

            var overwrittenBroadcastIds = new HashSet<Guid>();
            var now = DateTimeOffset.UtcNow;

            foreach (var curSch in currentSchedules)
            {
                foreach (var conf in conflictingDevices)
                {
                    if (scheduleDict.TryGetValue(conf.BroadcastScheduleId, out var schDates))
                    {
                        if (curSch.StartDate.Date <= schDates.EndDate.Date && curSch.EndDate.Date >= schDates.StartDate.Date)
                        {
                            conf.IsPreempted = true;
                            conf.PreemptedByBroadcastId = currentBroadcastId;
                            conf.DeviceStatus = "Tạm ngưng";
                            conf.UpdatedAt = now;

                            var bId = await _context.BroadcastSchedules
                                .Where(s => s.Id == conf.BroadcastScheduleId)
                                .Select(s => s.BroadcastId)
                                .FirstOrDefaultAsync();
                            if (bId != Guid.Empty)
                            {
                                overwrittenBroadcastIds.Add(bId);
                            }
                        }
                    }
                }
            }

            // GỬI THÔNG BÁO NTF-09 CHO NGƯỜI TẠO BẢN TIN BỊ ĐÈ
            if (overwrittenBroadcastIds.Any())
            {
                var overwrittenBroadcasts = await _context.Broadcasts
                    .Where(b => overwrittenBroadcastIds.Contains(b.Id) && !b.IsDeleted)
                    .ToListAsync();

                foreach (var ob in overwrittenBroadcasts)
                {
                    if (ob.CreatedBy.HasValue)
                    {
                        var notification = new SystemNotification
                        {
                            Id = Guid.NewGuid(),
                            OrganizationId = ob.OrganizationId,
                            FacilityId = ob.FacilityId,
                            Title = "Bản tin bị ghi đè phát sóng",
                            Content = $"Bản tin '{ob.BroadcastName}' ({ob.BroadcastCode}) của bạn đã bị ghi đè phát sóng bởi bản tin '{currentBroadcast.BroadcastName}' ({currentBroadcast.BroadcastCode}).",
                            NotificationType = "NTF-09",
                            TargetUserId = ob.CreatedBy.Value,
                            IsSent = true,
                            SentAt = now,
                            CreatedAt = now
                        };
                        _context.SystemNotifications.Add(notification);
                    }
                }
            }

            // Gửi lệnh ngắt phát tới các thiết bị đang online nếu có SignalR connection
            if (_connectionManager != null && _hubContext != null && _commandManager != null)
            {
                foreach (var devId in targetDeviceIds)
                {
                    if (_connectionManager.TryGetConnectionId(devId, out var connId) && !string.IsNullOrEmpty(connId))
                    {
                        try
                        {
                            var cmdId = Guid.NewGuid();
                            _commandManager.RegisterCommand(cmdId, devId, "StopBroadcast", "Pending");
                            await _hubContext.Clients.Client(connId).SendAsync("ReceiveCommand", new DeviceCommandEnvelope
                            {
                                CommandId = cmdId,
                                DeviceId = devId,
                                Type = "StopBroadcast",
                                Payload = new
                                {
                                    reason = "PreemptedByPriorityBroadcast",
                                    priorityBroadcastId = currentBroadcastId,
                                    stoppedAt = now
                                }
                            });
                            _commandManager.MarkSent(cmdId);
                        }
                        catch (Exception ex)
                        {
                            _logger?.LogWarning(ex, "Lỗi gửi lệnh dừng phát ngắt sóng tới thiết bị {DeviceId}", devId);
                        }
                    }
                }
            }

            await _context.SaveChangesAsync();
        }

        private async Task SendStopCommandToDevicesAsync(Broadcast broadcast, string reason)
        {
            if (_connectionManager == null || _hubContext == null || _commandManager == null)
                return;

            var deviceIds = broadcast.Schedules
                .SelectMany(s => s.BroadcastDevices)
                .Select(bd => bd.DeviceId)
                .Distinct()
                .ToList();

            if (!deviceIds.Any())
            {
                deviceIds = await _context.Devices
                    .AsNoTracking()
                    .Where(d => d.FacilityId == broadcast.FacilityId && d.ActiveStatus && !d.IsDeleted)
                    .Select(d => d.Id)
                    .ToListAsync();
            }

            var now = DateTimeOffset.UtcNow;
            foreach (var devId in deviceIds)
            {
                if (_connectionManager.TryGetConnectionId(devId, out var connId) && !string.IsNullOrEmpty(connId))
                {
                    try
                    {
                        var cmdId = Guid.NewGuid();
                        _commandManager.RegisterCommand(cmdId, devId, "StopBroadcast", "Pending");
                        await _hubContext.Clients.Client(connId).SendAsync("ReceiveCommand", new DeviceCommandEnvelope
                        {
                            CommandId = cmdId,
                            DeviceId = devId,
                            Type = "StopBroadcast",
                            Payload = new
                            {
                                reason = reason,
                                broadcastId = broadcast.Id,
                                stoppedAt = now
                            }
                        });
                        _commandManager.MarkSent(cmdId);
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogWarning(ex, "Lỗi gửi lệnh dừng phát tới thiết bị {DeviceId}", devId);
                    }
                }
            }
        }

        private async Task<HashSet<Guid>> GetAncestorFacilityIdsAsync(Guid facilityId)
        {
            var ancestors = new HashSet<Guid>();
            var allFacilities = await _context.Facilities
                .AsNoTracking()
                .Where(f => !f.IsDeleted && f.IsActive)
                .Select(f => new { f.Id, f.ParentId })
                .ToListAsync();

            var current = allFacilities.FirstOrDefault(f => f.Id == facilityId);
            var parentId = current?.ParentId;
            while (parentId.HasValue)
            {
                ancestors.Add(parentId.Value);
                var p = allFacilities.FirstOrDefault(f => f.Id == parentId.Value);
                parentId = p?.ParentId;
            }

            return ancestors;
        }

        private async Task<List<Guid>> GetAllSubFacilityIdsAsync(Guid parentFacilityId)
        {
            var allFacilities = await _context.Facilities
                .AsNoTracking()
                .Where(f => !f.IsDeleted && f.IsActive)
                .Select(f => new { f.Id, f.ParentId })
                .ToListAsync();

            var result = new List<Guid>();
            var childrenMap = allFacilities
                .Where(f => f.ParentId.HasValue)
                .GroupBy(f => f.ParentId!.Value)
                .ToDictionary(g => g.Key, g => g.Select(x => x.Id).ToList());

            var queue = new Queue<Guid>();
            queue.Enqueue(parentFacilityId);

            while (queue.TryDequeue(out var cur))
            {
                if (childrenMap.TryGetValue(cur, out var children))
                {
                    foreach (var c in children)
                    {
                        result.Add(c);
                        queue.Enqueue(c);
                    }
                }
            }

            return result;
        }
    }
}

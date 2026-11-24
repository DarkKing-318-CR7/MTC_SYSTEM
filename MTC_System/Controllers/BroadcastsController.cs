using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MTC_System.Common;
using MTC_System.Data;
using MTC_System.DTOs;
using MTC_System.Services;
using MTC_System.Services.Interfaces;

namespace MTC_System.Controllers
{
    /// <summary>
    /// [Dev 3] Controller Quản lý Bản tin & Lập lịch phát
    /// Đáp ứng tiêu chuẩn phân hệ USR-BRD-01 -> USR-BRD-11, Quản lý 9 trạng thái & Thuật toán quét trùng lịch (Issue #15)
    /// </summary>
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class BroadcastsController : ControllerBase
    {
        private readonly IBroadcastService _broadcastService;

        public BroadcastsController(IBroadcastService broadcastService)
        {
            _broadcastService = broadcastService;
        }

        private Guid? ResolveEffectiveFacilityId(Guid? requestingFacilityId)
        {
            if (requestingFacilityId.HasValue && requestingFacilityId.Value != Guid.Empty)
                return requestingFacilityId.Value;

            var userScope = User.GetUserScope();
            return userScope.FacilityId;
        }

        /// <summary>
        /// USR-BRD-01: Xem danh sách bản tin (Hỗ trợ phân trang, lọc theo Loại bản tin, Trạng thái, Cơ sở/Thiết bị, Thời gian bắt đầu phát, Từ khóa, Hình thức tạo)
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "ADMINISTRATOR,Administrator,CREATOR,Creator,APPROVER,Approver")]
        public async Task<IActionResult> GetList(
            [FromQuery] Guid? facilityId,
            [FromQuery] Guid? organizationId,
            [FromQuery] string? status,
            [FromQuery] string? broadcastType,
            [FromQuery] string? search,
            [FromQuery] int pageIndex = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] Guid? requestingFacilityId = null,
            [FromQuery] Guid? deviceId = null,
            [FromQuery] DateTimeOffset? startDateFrom = null,
            [FromQuery] DateTimeOffset? startDateTo = null,
            [FromQuery] DateTimeOffset? fromDate = null,
            [FromQuery] DateTimeOffset? toDate = null,
            [FromQuery] string? broadcastForm = null)
        {
            var userScope = User.GetUserScope();
            if (!userScope.IsSystemAdministrator && userScope.OrganizationId.HasValue)
            {
                organizationId = userScope.OrganizationId.Value;
            }

            var effectiveFrom = startDateFrom ?? fromDate;
            var effectiveTo = startDateTo ?? toDate;

            var effectiveFacilityId = ResolveEffectiveFacilityId(requestingFacilityId);
            var res = await _broadcastService.GetListAsync(
                facilityId,
                organizationId,
                status,
                broadcastType,
                search,
                pageIndex,
                pageSize,
                effectiveFacilityId,
                deviceId,
                effectiveFrom,
                effectiveTo,
                broadcastForm);

            return Ok(res);
        }

        /// <summary>
        /// USR-BRD-03: Xem thông tin bản tin chi tiết (Bản tin của cơ sở cha cho phép cơ sở con xem chi tiết theo USR-BRD-01)
        /// </summary>
        [HttpGet("{id}")]
        [Authorize(Roles = "ADMINISTRATOR,Administrator,CREATOR,Creator,APPROVER,Approver")]
        public async Task<IActionResult> GetById(Guid id, [FromQuery] Guid? requestingFacilityId = null)
        {
            var effectiveFacilityId = ResolveEffectiveFacilityId(requestingFacilityId);
            var res = await _broadcastService.GetByIdAsync(id, effectiveFacilityId);
            if (!res.Success) return NotFound(res);
            return Ok(res);
        }

        /// <summary>
        /// USR-BRD-Content: Lấy nội dung đa phương tiện hoặc cấu hình phát sóng của bản tin
        /// Yêu cầu xác thực: User Bearer JWT hoặc DeviceBearer JWT (kiểm tra phân quyền phạm vi tổ chức)
        /// </summary>
        [HttpGet("{id:guid}/content")]
        [Authorize(AuthenticationSchemes = $"{Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme},DeviceBearer")]
        public async Task<IActionResult> GetContent(Guid id, [FromQuery] Guid? requestingFacilityId = null)
        {
            var effectiveFacilityId = ResolveEffectiveFacilityId(requestingFacilityId);
            var res = await _broadcastService.GetByIdAsync(id, effectiveFacilityId);
            if (!res.Success || res.Data == null) return NotFound(res);

            var broadcast = res.Data;

            // Kiểm tra phân quyền phạm vi tổ chức (Chặn truy cập chéo tổ chức)
            bool isAdmin = User.IsInRole("ADMINISTRATOR") || User.IsInRole("Administrator");
            if (!isAdmin)
            {
                var tokenVersion = User.FindFirst("token_version")?.Value;
                if (!string.IsNullOrEmpty(tokenVersion))
                {
                    // Caller là Device
                    var deviceIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    if (!Guid.TryParse(deviceIdClaim, out var deviceId)) return Forbid();

                    var dbContext = HttpContext.RequestServices.GetRequiredService<MyDbContext>();
                    var device = await dbContext.Devices.AsNoTracking().FirstOrDefaultAsync(d => d.Id == deviceId && !d.IsDeleted);
                    if (device == null || device.OrganizationId != broadcast.OrganizationId || !device.ActiveStatus || !device.IsActivated)
                        return Forbid();

                    bool isAssigned = await dbContext.BroadcastDevices.AsNoTracking().AnyAsync(bd =>
                        bd.DeviceId == device.Id && !bd.IsDeleted &&
                        dbContext.BroadcastSchedules.Any(s => s.Id == bd.BroadcastScheduleId && s.BroadcastId == broadcast.Id && !s.IsDeleted));

                    if (!isAssigned)
                    {
                        var mediaService = HttpContext.RequestServices.GetRequiredService<IMediaService>();
                        bool canAccessFacility = await mediaService.CanAccessFacilityAsync(device.OrganizationId, broadcast.FacilityId, device.FacilityId, HttpContext.RequestAborted);
                        if (!canAccessFacility)
                            return Forbid();
                    }
                }
                else
                {
                    // Caller là User
                    var orgClaim = User.FindFirst("organizationId")?.Value;
                    if (!Guid.TryParse(orgClaim, out var userOrgId) || userOrgId != broadcast.OrganizationId)
                        return Forbid();

                    var facClaim = User.FindFirst("facilityId")?.Value;
                    if (!string.IsNullOrWhiteSpace(facClaim) && Guid.TryParse(facClaim, out var userFacilityId))
                    {
                        var mediaService = HttpContext.RequestServices.GetRequiredService<IMediaService>();
                        bool canAccessFacility = await mediaService.CanAccessFacilityAsync(userOrgId, userFacilityId, broadcast.FacilityId, HttpContext.RequestAborted);
                        if (!canAccessFacility)
                            return Forbid();
                    }
                }
            }

            if (broadcast.MediaId.HasValue)
            {
                return Redirect($"/api/media/{broadcast.MediaId.Value}/stream");
            }

            return Ok(ApiResponse<object>.Ok(new
            {
                broadcastId = broadcast.Id,
                broadcastName = broadcast.BroadcastName,
                broadcastCode = broadcast.BroadcastCode,
                mediaId = broadcast.MediaId,
                playlistId = broadcast.PlaylistId,
                status = broadcast.Status
            }, "Nội dung phát sóng của bản tin."));
        }

        /// <summary>
        /// USR-BRD-02: Thêm bản tin (Kiểm tra trùng lịch theo Chế độ Cơ sở/Thiết bị, khóa thời lượng Playlist, hỗ trợ phát qua ngày)
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "ADMINISTRATOR,Administrator,CREATOR,Creator")]
        public async Task<IActionResult> Create([FromBody] CreateBroadcastDto dto)
        {
            var userScope = User.GetUserScope();
            if (dto.OrganizationId == Guid.Empty && userScope.OrganizationId.HasValue)
            {
                dto.OrganizationId = userScope.OrganizationId.Value;
            }
            if (dto.FacilityId == Guid.Empty && userScope.FacilityId.HasValue)
            {
                dto.FacilityId = userScope.FacilityId.Value;
            }

            var res = await _broadcastService.CreateAsync(dto);
            if (!res.Success) return BadRequest(res);
            return CreatedAtAction(nameof(GetById), new { id = res.Data!.Id }, res);
        }

        /// <summary>
        /// Thuật toán Quét Trùng Lịch (Conflict Detection Engine - Issue #15):
        /// Kiểm tra trước xung đột lịch phát theo chế độ Cơ sở / Thiết bị trước khi bấm lưu
        /// </summary>
        [HttpPost("check-conflict")]
        [Authorize(Roles = "ADMINISTRATOR,Administrator,CREATOR,Creator,APPROVER,Approver")]
        public async Task<IActionResult> CheckConflict([FromBody] CheckConflictRequestDto dto)
        {
            var userScope = User.GetUserScope();
            if (dto.OrganizationId == Guid.Empty && userScope.OrganizationId.HasValue)
            {
                dto.OrganizationId = userScope.OrganizationId.Value;
            }

            var res = await _broadcastService.CheckConflictApiAsync(dto);
            return Ok(res);
        }

        /// <summary>
        /// USR-BRD-04: Chỉnh sửa bản tin (Chỉ sửa bản tin Chờ duyệt, chặn cơ sở con sửa bản tin của cha - USR-BRD-01)
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "ADMINISTRATOR,Administrator,CREATOR,Creator")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateBroadcastDto dto, [FromQuery] Guid? requestingFacilityId = null)
        {
            var effectiveFacilityId = ResolveEffectiveFacilityId(requestingFacilityId);
            var res = await _broadcastService.UpdateAsync(id, dto, effectiveFacilityId);
            if (!res.Success) return BadRequest(res);
            return Ok(res);
        }

        /// <summary>
        /// USR-BRD-05: Xóa bản tin (Chỉ xóa bản tin Chờ duyệt, Từ chối duyệt hoặc Đã hủy; chặn cơ sở con xóa của cha - USR-BRD-01)
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "ADMINISTRATOR,Administrator,CREATOR,Creator")]
        public async Task<IActionResult> Delete(Guid id, [FromQuery] Guid? requestingFacilityId = null)
        {
            var effectiveFacilityId = ResolveEffectiveFacilityId(requestingFacilityId);
            var res = await _broadcastService.DeleteAsync(id, effectiveFacilityId);
            if (!res.Success) return BadRequest(res);
            return Ok(res);
        }

        /// <summary>
        /// USR-BRD-06: Duyệt bản tin (Chỉ duyệt bản tin Chờ duyệt; bản tin ưu tiên duyệt sau phát đè lên bản tin duyệt trước)
        /// </summary>
        [HttpPost("{id}/approve")]
        [Authorize(Roles = "ADMINISTRATOR,Administrator,APPROVER,Approver")]
        public async Task<IActionResult> Approve(Guid id, [FromBody] ApproveBroadcastDto dto, [FromQuery] Guid? requestingFacilityId = null)
        {
            var userScope = User.GetUserScope();
            if (!dto.ApprovedBy.HasValue && userScope.UserId.HasValue)
            {
                dto.ApprovedBy = userScope.UserId;
            }

            var effectiveFacilityId = ResolveEffectiveFacilityId(requestingFacilityId);
            var res = await _broadcastService.ApproveAsync(id, dto, effectiveFacilityId);
            if (!res.Success) return BadRequest(res);
            return Ok(res);
        }

        /// <summary>
        /// USR-BRD-07: Từ chối phê duyệt (Chỉ với bản tin Chờ duyệt. Lý do tối đa 510 ký tự)
        /// </summary>
        [HttpPost("{id}/reject")]
        [Authorize(Roles = "ADMINISTRATOR,Administrator,APPROVER,Approver")]
        public async Task<IActionResult> Reject(Guid id, [FromBody] RejectBroadcastDto dto, [FromQuery] Guid? requestingFacilityId = null)
        {
            var userScope = User.GetUserScope();
            if (!dto.RejectedBy.HasValue && userScope.UserId.HasValue)
            {
                dto.RejectedBy = userScope.UserId;
            }

            var effectiveFacilityId = ResolveEffectiveFacilityId(requestingFacilityId);
            var res = await _broadcastService.RejectAsync(id, dto, effectiveFacilityId);
            if (!res.Success) return BadRequest(res);
            return Ok(res);
        }

        /// <summary>
        /// USR-BRD-08: Hủy bản tin (Chỉ hủy bản tin Đã duyệt)
        /// </summary>
        [HttpPost("{id}/cancel")]
        [Authorize(Roles = "ADMINISTRATOR,Administrator,APPROVER,Approver,CREATOR,Creator")]
        public async Task<IActionResult> Cancel(Guid id, [FromBody] CancelBroadcastDto dto, [FromQuery] Guid? requestingFacilityId = null)
        {
            var effectiveFacilityId = ResolveEffectiveFacilityId(requestingFacilityId);
            var res = await _broadcastService.CancelAsync(id, dto, effectiveFacilityId);
            if (!res.Success) return BadRequest(res);
            return Ok(res);
        }

        /// <summary>
        /// USR-BRD-09: Dừng phát (Chỉ với bản tin Đang phát; không áp dụng cho bản tin ưu tiên)
        /// </summary>
        [HttpPost("{id}/pause")]
        [Authorize(Roles = "ADMINISTRATOR,Administrator,APPROVER,Approver,CREATOR,Creator")]
        public async Task<IActionResult> Pause(Guid id, [FromBody] PauseBroadcastDto dto, [FromQuery] Guid? requestingFacilityId = null)
        {
            var effectiveFacilityId = ResolveEffectiveFacilityId(requestingFacilityId);
            var res = await _broadcastService.PauseAsync(id, dto, effectiveFacilityId);
            if (!res.Success) return BadRequest(res);
            return Ok(res);
        }

        /// <summary>
        /// USR-BRD-10: Tiếp tục phát (Chỉ với bản tin Tạm ngưng; không áp dụng cho bản tin ưu tiên)
        /// </summary>
        [HttpPost("{id}/resume")]
        [Authorize(Roles = "ADMINISTRATOR,Administrator,APPROVER,Approver,CREATOR,Creator")]
        public async Task<IActionResult> Resume(Guid id, [FromQuery] Guid? requestingFacilityId = null)
        {
            var effectiveFacilityId = ResolveEffectiveFacilityId(requestingFacilityId);
            var res = await _broadcastService.ResumeAsync(id, effectiveFacilityId);
            if (!res.Success) return BadRequest(res);
            return Ok(res);
        }

        /// <summary>
        /// USR-BRD-11: Cơ sở cha xem danh sách bản tin của cơ sở con (tự động nhận diện cơ sở cha từ Token) kèm bộ lọc
        /// </summary>
        [HttpGet("sub-facilities")]
        [Authorize(Roles = "ADMINISTRATOR,Administrator,APPROVER,Approver,CREATOR,Creator")]
        public async Task<IActionResult> GetSubFacilityBroadcastsCurrentUser(
            [FromQuery] string? search,
            [FromQuery] string? status,
            [FromQuery] string? broadcastType,
            [FromQuery] int pageIndex = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] Guid? subFacilityId = null,
            [FromQuery] Guid? deviceId = null,
            [FromQuery] DateTimeOffset? startDateFrom = null,
            [FromQuery] DateTimeOffset? startDateTo = null,
            [FromQuery] DateTimeOffset? fromDate = null,
            [FromQuery] DateTimeOffset? toDate = null,
            [FromQuery] string? broadcastForm = null)
        {
            var userScope = User.GetUserScope();
            if (!userScope.FacilityId.HasValue && !userScope.IsSystemAdministrator)
            {
                return BadRequest(ApiResponse<object>.Fail("Người dùng không thuộc cơ sở nào!"));
            }

            var facilityId = userScope.FacilityId ?? Guid.Empty;
            var effectiveFrom = startDateFrom ?? fromDate;
            var effectiveTo = startDateTo ?? toDate;

            var res = await _broadcastService.GetSubFacilityBroadcastsAsync(
                facilityId,
                search,
                status,
                broadcastType,
                pageIndex,
                pageSize,
                subFacilityId,
                deviceId,
                effectiveFrom,
                effectiveTo,
                broadcastForm);

            return Ok(res);
        }

        /// <summary>
        /// USR-BRD-11: Cơ sở cha xem danh sách bản tin của cơ sở con theo parentFacilityId kèm bộ lọc
        /// </summary>
        [HttpGet("sub-facilities/{parentFacilityId}")]
        [Authorize(Roles = "ADMINISTRATOR,Administrator,APPROVER,Approver,CREATOR,Creator")]
        public async Task<IActionResult> GetSubFacilityBroadcasts(
            Guid parentFacilityId,
            [FromQuery] string? search,
            [FromQuery] string? status,
            [FromQuery] string? broadcastType,
            [FromQuery] int pageIndex = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] Guid? subFacilityId = null,
            [FromQuery] Guid? deviceId = null,
            [FromQuery] DateTimeOffset? startDateFrom = null,
            [FromQuery] DateTimeOffset? startDateTo = null,
            [FromQuery] DateTimeOffset? fromDate = null,
            [FromQuery] DateTimeOffset? toDate = null,
            [FromQuery] string? broadcastForm = null)
        {
            var effectiveFrom = startDateFrom ?? fromDate;
            var effectiveTo = startDateTo ?? toDate;

            var res = await _broadcastService.GetSubFacilityBroadcastsAsync(
                parentFacilityId,
                search,
                status,
                broadcastType,
                pageIndex,
                pageSize,
                subFacilityId,
                deviceId,
                effectiveFrom,
                effectiveTo,
                broadcastForm);

            return Ok(res);
        }

        /// <summary>
        /// USR-BRD-11: Cơ sở cha xem chi tiết bản tin của cơ sở con
        /// </summary>
        [HttpGet("sub-facilities/{parentFacilityId}/{broadcastId}")]
        [Authorize(Roles = "ADMINISTRATOR,Administrator,APPROVER,Approver,CREATOR,Creator")]
        public async Task<IActionResult> GetSubFacilityBroadcastDetail(
            Guid parentFacilityId,
            Guid broadcastId)
        {
            var res = await _broadcastService.GetSubFacilityBroadcastDetailAsync(parentFacilityId, broadcastId);
            if (!res.Success) return NotFound(res);
            return Ok(res);
        }

        // =========================================================
        // QUẢN LÝ LỊCH PHÁT (BROADCAST SCHEDULES)
        // =========================================================

        /// <summary>
        /// Lấy toàn bộ danh sách lịch phát của bản tin
        /// </summary>
        [HttpGet("{id}/schedules")]
        [Authorize(Roles = "ADMINISTRATOR,Administrator,CREATOR,Creator,APPROVER,Approver")]
        public async Task<IActionResult> GetSchedules(Guid id)
        {
            var res = await _broadcastService.GetSchedulesAsync(id);
            return Ok(res);
        }

        /// <summary>
        /// Thêm lịch phát mới cho bản tin (quét trùng lịch, kiểm tra phát qua ngày)
        /// </summary>
        [HttpPost("{id}/schedules")]
        [Authorize(Roles = "ADMINISTRATOR,Administrator,CREATOR,Creator")]
        public async Task<IActionResult> AddSchedule(Guid id, [FromBody] CreateBroadcastScheduleDto dto)
        {
            var res = await _broadcastService.AddScheduleAsync(id, dto);
            if (!res.Success) return BadRequest(res);
            return Ok(res);
        }

        /// <summary>
        /// Cập nhật lịch phát (chặn sửa nếu IsLocked = true)
        /// </summary>
        [HttpPut("schedules/{scheduleId}")]
        [Authorize(Roles = "ADMINISTRATOR,Administrator,CREATOR,Creator")]
        public async Task<IActionResult> UpdateSchedule(Guid scheduleId, [FromBody] UpdateBroadcastScheduleDto dto)
        {
            var res = await _broadcastService.UpdateScheduleAsync(scheduleId, dto);
            if (!res.Success) return BadRequest(res);
            return Ok(res);
        }

        /// <summary>
        /// Xóa lịch phát (chặn xóa nếu IsLocked = true)
        /// </summary>
        [HttpDelete("schedules/{scheduleId}")]
        [Authorize(Roles = "ADMINISTRATOR,Administrator,CREATOR,Creator")]
        public async Task<IActionResult> DeleteSchedule(Guid scheduleId)
        {
            var res = await _broadcastService.DeleteScheduleAsync(scheduleId);
            if (!res.Success) return BadRequest(res);
            return Ok(res);
        }
    }
}

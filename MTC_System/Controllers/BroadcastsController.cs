using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using MTC_System.DTOs;
using MTC_System.Services;

namespace MTC_System.Controllers
{
    /// <summary>
    /// [Dev 3] Controller Quản lý Bản tin & Lập lịch phát
    /// Đáp ứng tiêu chuẩn phân hệ USR-BRD-01 -> USR-BRD-11 & Thuật toán quét trùng lịch (Issue #15)
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class BroadcastsController : ControllerBase
    {
        private readonly IBroadcastService _broadcastService;

        public BroadcastsController(IBroadcastService broadcastService)
        {
            _broadcastService = broadcastService;
        }

        /// <summary>
        /// USR-BRD-01: Xem danh sách bản tin (Hỗ trợ phân trang, lọc, tìm kiếm, tính toán cờ quyền hành động và cơ sở cha-con)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetList(
            [FromQuery] Guid? facilityId,
            [FromQuery] Guid? organizationId,
            [FromQuery] string? status,
            [FromQuery] string? search,
            [FromQuery] int pageIndex = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] Guid? requestingFacilityId = null)
        {
            var res = await _broadcastService.GetListAsync(facilityId, organizationId, status, search, pageIndex, pageSize, requestingFacilityId);
            return Ok(res);
        }

        /// <summary>
        /// USR-BRD-03: Xem thông tin bản tin chi tiết
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id, [FromQuery] Guid? requestingFacilityId = null)
        {
            var res = await _broadcastService.GetByIdAsync(id, requestingFacilityId);
            if (!res.Success) return NotFound(res);
            return Ok(res);
        }

        /// <summary>
        /// USR-BRD-02: Thêm bản tin (Kiểm tra trùng lịch theo Chế độ Cơ sở/Thiết bị, khóa thời lượng Playlist, hỗ trợ phát qua ngày)
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateBroadcastDto dto)
        {
            var res = await _broadcastService.CreateAsync(dto);
            if (!res.Success) return BadRequest(res);
            return CreatedAtAction(nameof(GetById), new { id = res.Data!.Id }, res);
        }

        /// <summary>
        /// Thuật toán Quét Trùng Lịch (Conflict Detection Engine - Issue #15):
        /// Kiểm tra trước xung đột lịch phát theo chế độ Cơ sở / Thiết bị trước khi bấm lưu
        /// </summary>
        [HttpPost("check-conflict")]
        public async Task<IActionResult> CheckConflict([FromBody] CheckConflictRequestDto dto)
        {
            var res = await _broadcastService.CheckConflictApiAsync(dto);
            return Ok(res);
        }

        /// <summary>
        /// USR-BRD-04: Chỉnh sửa bản tin (Chỉ sửa bản tin Chờ duyệt, chặn cơ sở con sửa bản tin của cha)
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateBroadcastDto dto, [FromQuery] Guid? requestingFacilityId = null)
        {
            var res = await _broadcastService.UpdateAsync(id, dto, requestingFacilityId);
            if (!res.Success) return BadRequest(res);
            return Ok(res);
        }

        /// <summary>
        /// USR-BRD-05: Xóa bản tin (Chỉ xóa bản tin Chờ duyệt, Từ chối duyệt hoặc Đã hủy; chặn cơ sở con xóa của cha)
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id, [FromQuery] Guid? requestingFacilityId = null)
        {
            var res = await _broadcastService.DeleteAsync(id, requestingFacilityId);
            if (!res.Success) return BadRequest(res);
            return Ok(res);
        }

        /// <summary>
        /// USR-BRD-06: Duyệt bản tin (Chỉ duyệt bản tin Chờ duyệt; bản tin ưu tiên duyệt sau phát đè lên bản tin duyệt trước)
        /// </summary>
        [HttpPost("{id}/approve")]
        public async Task<IActionResult> Approve(Guid id, [FromBody] ApproveBroadcastDto dto)
        {
            var res = await _broadcastService.ApproveAsync(id, dto);
            if (!res.Success) return BadRequest(res);
            return Ok(res);
        }

        /// <summary>
        /// USR-BRD-07: Từ chối phê duyệt (Chỉ với bản tin Chờ duyệt. Lý do tối đa 510 ký tự)
        /// </summary>
        [HttpPost("{id}/reject")]
        public async Task<IActionResult> Reject(Guid id, [FromBody] RejectBroadcastDto dto)
        {
            var res = await _broadcastService.RejectAsync(id, dto);
            if (!res.Success) return BadRequest(res);
            return Ok(res);
        }

        /// <summary>
        /// USR-BRD-08: Hủy bản tin (Chỉ hủy bản tin Đã duyệt)
        /// </summary>
        [HttpPost("{id}/cancel")]
        public async Task<IActionResult> Cancel(Guid id, [FromBody] CancelBroadcastDto dto, [FromQuery] Guid? requestingFacilityId = null)
        {
            var res = await _broadcastService.CancelAsync(id, dto, requestingFacilityId);
            if (!res.Success) return BadRequest(res);
            return Ok(res);
        }

        /// <summary>
        /// USR-BRD-09: Dừng phát (Chỉ với bản tin Đang phát; không áp dụng cho bản tin ưu tiên)
        /// </summary>
        [HttpPost("{id}/pause")]
        public async Task<IActionResult> Pause(Guid id, [FromBody] PauseBroadcastDto dto)
        {
            var res = await _broadcastService.PauseAsync(id, dto);
            if (!res.Success) return BadRequest(res);
            return Ok(res);
        }

        /// <summary>
        /// USR-BRD-10: Tiếp tục phát (Chỉ với bản tin Tạm ngưng)
        /// </summary>
        [HttpPost("{id}/resume")]
        public async Task<IActionResult> Resume(Guid id)
        {
            var res = await _broadcastService.ResumeAsync(id);
            if (!res.Success) return BadRequest(res);
            return Ok(res);
        }

        /// <summary>
        /// USR-BRD-11: Bản tin cơ sở con (Xem danh sách và thông tin bản tin của cơ sở con)
        /// </summary>
        [HttpGet("sub-facilities/{parentFacilityId}")]
        public async Task<IActionResult> GetSubFacilityBroadcasts(
            Guid parentFacilityId,
            [FromQuery] string? search,
            [FromQuery] int pageIndex = 1,
            [FromQuery] int pageSize = 10)
        {
            var res = await _broadcastService.GetSubFacilityBroadcastsAsync(parentFacilityId, search, pageIndex, pageSize);
            return Ok(res);
        }

        // =========================================================
        // QUẢN LÝ LỊCH PHÁT (BROADCAST SCHEDULES)
        // =========================================================

        /// <summary>
        /// Lấy toàn bộ danh sách lịch phát của bản tin
        /// </summary>
        [HttpGet("{id}/schedules")]
        public async Task<IActionResult> GetSchedules(Guid id)
        {
            var res = await _broadcastService.GetSchedulesAsync(id);
            return Ok(res);
        }

        /// <summary>
        /// Thêm lịch phát mới cho bản tin (quét trùng lịch, kiểm tra phát qua ngày)
        /// </summary>
        [HttpPost("{id}/schedules")]
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
        public async Task<IActionResult> DeleteSchedule(Guid scheduleId)
        {
            var res = await _broadcastService.DeleteScheduleAsync(scheduleId);
            if (!res.Success) return BadRequest(res);
            return Ok(res);
        }
    }
}

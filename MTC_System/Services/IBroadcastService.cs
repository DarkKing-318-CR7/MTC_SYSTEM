using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MTC_System.Common;
using MTC_System.DTOs;

namespace MTC_System.Services
{
    public interface IBroadcastService
    {
        // USR-BRD-01: Xem danh sách bản tin (tính toán cờ hành động phân quyền & cha-con)
        Task<ApiResponse<PagedResult<BroadcastListResponseDto>>> GetListAsync(
            Guid? facilityId,
            Guid? organizationId,
            string? status,
            string? search,
            int pageIndex = 1,
            int pageSize = 10,
            Guid? requestingFacilityId = null);

        // USR-BRD-03: Xem thông tin chi tiết bản tin
        Task<ApiResponse<BroadcastDetailResponseDto>> GetByIdAsync(Guid id, Guid? requestingFacilityId = null);

        // USR-BRD-02: Thêm bản tin (kiểm tra trùng lịch theo Cơ sở / Thiết bị, khóa playlist duration, hỗ trợ phát qua ngày)
        Task<ApiResponse<BroadcastDetailResponseDto>> CreateAsync(CreateBroadcastDto dto);

        // API kiểm tra trước trùng lịch (Conflict Detection Engine - Issue #15)
        Task<ApiResponse<ConflictCheckResultDto>> CheckConflictApiAsync(CheckConflictRequestDto dto);

        // USR-BRD-04: Chỉnh sửa bản tin (Chỉ sửa bản tin Chờ duyệt, chặn con sửa cha)
        Task<ApiResponse<BroadcastDetailResponseDto>> UpdateAsync(Guid id, UpdateBroadcastDto dto, Guid? requestingFacilityId = null);

        // USR-BRD-05: Xóa bản tin (Chỉ xóa bản tin Chờ duyệt, Từ chối duyệt hoặc Đã hủy)
        Task<ApiResponse<bool>> DeleteAsync(Guid id, Guid? requestingFacilityId = null);

        // USR-BRD-06: Duyệt bản tin (Chỉ duyệt bản tin Chờ duyệt; tin ưu tiên đè tin trước)
        Task<ApiResponse<BroadcastDetailResponseDto>> ApproveAsync(Guid id, ApproveBroadcastDto dto);

        // USR-BRD-07: Từ chối phê duyệt (Chỉ với bản tin Chờ duyệt; lý do tối đa 510 ký tự)
        Task<ApiResponse<BroadcastDetailResponseDto>> RejectAsync(Guid id, RejectBroadcastDto dto);

        // USR-BRD-08: Hủy bản tin (Chỉ hủy bản tin Đã duyệt)
        Task<ApiResponse<bool>> CancelAsync(Guid id, CancelBroadcastDto dto, Guid? requestingFacilityId = null);

        // USR-BRD-09: Dừng phát (Chỉ với bản tin Đang phát; không áp dụng cho bản tin ưu tiên)
        Task<ApiResponse<BroadcastDetailResponseDto>> PauseAsync(Guid id, PauseBroadcastDto dto);

        // USR-BRD-10: Tiếp tục phát (Chỉ với bản tin Tạm ngưng)
        Task<ApiResponse<BroadcastDetailResponseDto>> ResumeAsync(Guid id);

        // USR-BRD-11: Bản tin cơ sở con (Xem danh sách và thông tin bản tin của cơ sở con)
        Task<ApiResponse<PagedResult<BroadcastListResponseDto>>> GetSubFacilityBroadcastsAsync(
            Guid parentFacilityId,
            string? search,
            int pageIndex = 1,
            int pageSize = 10);

        // Quản lý lịch phát
        Task<ApiResponse<List<BroadcastScheduleResponseDto>>> GetSchedulesAsync(Guid broadcastId);
        Task<ApiResponse<BroadcastScheduleResponseDto>> AddScheduleAsync(Guid broadcastId, CreateBroadcastScheduleDto dto);
        Task<ApiResponse<BroadcastScheduleResponseDto>> UpdateScheduleAsync(Guid scheduleId, UpdateBroadcastScheduleDto dto);
        Task<ApiResponse<bool>> DeleteScheduleAsync(Guid scheduleId);
    }
}

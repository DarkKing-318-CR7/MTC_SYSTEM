using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MTC_System.DTOs
{
    // =========================================================
    // DTOs CHO BẢN TIN (BROADCAST)
    // =========================================================

    public class CreateBroadcastDto
    {
        public string? BroadcastCode { get; set; }

        [Required(ErrorMessage = "Tên bản tin là bắt buộc")]
        [MaxLength(255, ErrorMessage = "Tên bản tin không được vượt quá 255 ký tự")]
        public string BroadcastName { get; set; } = string.Empty;

        [MaxLength(2000, ErrorMessage = "Mô tả không được vượt quá 2000 ký tự")]
        public string? Description { get; set; }

        /// <summary>Loại bản tin: "Thường" | "Khẩn cấp"</summary>
        public string BroadcastType { get; set; } = "Thường";

        /// <summary>Độ ưu tiên: 1 = Khẩn cấp/Ưu tiên cao (được phát qua ngày, ghi đè), 2 = Thường</summary>
        public int Priority { get; set; } = 2;

        /// <summary>Loại nội dung: "File" | "Playlist" | "URL" | "Stream" | "FM"</summary>
        [Required(ErrorMessage = "Loại nội dung là bắt buộc")]
        public string ContentType { get; set; } = string.Empty;

        public Guid? MediaId { get; set; }
        public Guid? PlaylistId { get; set; }

        [Required(ErrorMessage = "OrganizationId là bắt buộc")]
        public Guid OrganizationId { get; set; }

        [Required(ErrorMessage = "FacilityId là bắt buộc")]
        public Guid FacilityId { get; set; }

        /// <summary>Cờ cho phép ghi đè (Overwrite = true) khi phát hiện trùng lịch (USR-BRD-02 / Issue #15)</summary>
        public bool Overwrite { get; set; } = false;

        /// <summary>Lịch phát đi kèm khi tạo bản tin (tùy chọn)</summary>
        public List<CreateBroadcastScheduleDto>? Schedules { get; set; }
    }

    public class UpdateBroadcastDto
    {
        [Required(ErrorMessage = "Tên bản tin là bắt buộc")]
        [MaxLength(255, ErrorMessage = "Tên bản tin không được vượt quá 255 ký tự")]
        public string BroadcastName { get; set; } = string.Empty;

        [MaxLength(2000, ErrorMessage = "Mô tả không được vượt quá 2000 ký tự")]
        public string? Description { get; set; }

        public string BroadcastType { get; set; } = "Thường";
        public int Priority { get; set; } = 2;

        [Required(ErrorMessage = "Loại nội dung là bắt buộc")]
        public string ContentType { get; set; } = string.Empty;

        public Guid? MediaId { get; set; }
        public Guid? PlaylistId { get; set; }
    }

    /// <summary>USR-BRD-06: Duyệt bản tin</summary>
    public class ApproveBroadcastDto
    {
        public Guid? ApprovedBy { get; set; }
        /// <summary>Cho phép ghi đè các bản tin cũ nếu đây là bản tin ưu tiên (USR-BRD-01)</summary>
        public bool Overwrite { get; set; } = false;
    }

    /// <summary>USR-BRD-07: Từ chối phê duyệt (Lý do tối đa 510 ký tự)</summary>
    public class RejectBroadcastDto
    {
        [Required(ErrorMessage = "Bắt buộc phải nhập lý do từ chối")]
        [MaxLength(510, ErrorMessage = "Lý do từ chối tối đa 510 ký tự")]
        public string RejectionReason { get; set; } = string.Empty;

        public Guid? RejectedBy { get; set; }
    }

    /// <summary>USR-BRD-08: Hủy bản tin</summary>
    public class CancelBroadcastDto
    {
        [MaxLength(500, ErrorMessage = "Lý do hủy không được quá 500 ký tự")]
        public string? Reason { get; set; }
    }

    /// <summary>USR-BRD-09: Tạm dừng phát bản tin</summary>
    public class PauseBroadcastDto
    {
        [MaxLength(500, ErrorMessage = "Lý do tạm dừng không được quá 500 ký tự")]
        public string? Reason { get; set; }
    }

    public class BroadcastListResponseDto
    {
        public Guid Id { get; set; }
        public string BroadcastCode { get; set; } = string.Empty;
        public string BroadcastName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string BroadcastType { get; set; } = string.Empty;
        public int Priority { get; set; }
        public string Status { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public Guid OrganizationId { get; set; }
        public Guid FacilityId { get; set; }
        public string? FacilityName { get; set; }
        public DateTimeOffset? ApprovedAt { get; set; }
        public Guid? ApprovedBy { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public int TotalSchedules { get; set; }

        // ================= ACTION PERMISSIONS (USR-BRD-01) =================
        public bool CanEdit { get; set; }
        public bool CanDelete { get; set; }
        public bool CanApprove { get; set; }
        public bool CanReject { get; set; }
        public bool CanCancel { get; set; }
        public bool CanPause { get; set; }
        public bool CanResume { get; set; }
        public bool IsFromParent { get; set; }
        public bool IsPriority => Priority == 1 || BroadcastType == "Khẩn cấp";
    }

    public class BroadcastDetailResponseDto
    {
        public Guid Id { get; set; }
        public string BroadcastCode { get; set; } = string.Empty;
        public string BroadcastName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string BroadcastType { get; set; } = string.Empty;
        public int Priority { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? RejectionReason { get; set; }
        public string ContentType { get; set; } = string.Empty;
        public Guid? MediaId { get; set; }
        public string? MediaName { get; set; }
        public Guid? PlaylistId { get; set; }
        public string? PlaylistName { get; set; }
        public Guid OrganizationId { get; set; }
        public Guid FacilityId { get; set; }
        public string? FacilityName { get; set; }
        public DateTimeOffset? ApprovedAt { get; set; }
        public Guid? ApprovedBy { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }

        // ================= ACTION PERMISSIONS (USR-BRD-01) =================
        public bool CanEdit { get; set; }
        public bool CanDelete { get; set; }
        public bool CanApprove { get; set; }
        public bool CanReject { get; set; }
        public bool CanCancel { get; set; }
        public bool CanPause { get; set; }
        public bool CanResume { get; set; }
        public bool IsFromParent { get; set; }
        public bool IsPriority => Priority == 1 || BroadcastType == "Khẩn cấp";

        public List<BroadcastScheduleResponseDto> Schedules { get; set; } = new List<BroadcastScheduleResponseDto>();
    }

    // =========================================================
    // DTOs CHO LỊCH PHÁT (BROADCAST SCHEDULE)
    // =========================================================

    public class CreateBroadcastScheduleDto
    {
        [Required(ErrorMessage = "Ngày bắt đầu phát là bắt buộc")]
        public DateTimeOffset StartDate { get; set; }

        [Required(ErrorMessage = "Ngày kết thúc phát là bắt buộc")]
        public DateTimeOffset EndDate { get; set; }

        /// <summary>"Không lặp" | "Hàng ngày" | "Theo tuần" | "Theo tháng"</summary>
        public string RepeatType { get; set; } = "Không lặp";

        /// <summary>"2,3,4,5,6,7,CN" hoặc "1,15,28"</summary>
        public string? RepeatDays { get; set; }

        /// <summary>Ghi đè nếu trùng lịch</summary>
        public bool Overwrite { get; set; } = false;

        /// <summary>Danh sách các khung giờ phát trong ngày</summary>
        public List<CreateBroadcastTimeSlotDto> TimeSlots { get; set; } = new List<CreateBroadcastTimeSlotDto>();

        /// <summary>Danh sách thiết bị gán phát (nếu chế độ Theo cơ sở, có thể để trống để tự động gán toàn bộ)</summary>
        public List<Guid>? DeviceIds { get; set; }
    }

    public class UpdateBroadcastScheduleDto
    {
        public DateTimeOffset StartDate { get; set; }
        public DateTimeOffset EndDate { get; set; }
        public string RepeatType { get; set; } = "Không lặp";
        public string? RepeatDays { get; set; }
        public bool Overwrite { get; set; } = false;
        public List<CreateBroadcastTimeSlotDto> TimeSlots { get; set; } = new List<CreateBroadcastTimeSlotDto>();
        public List<Guid>? DeviceIds { get; set; }
    }

    public class BroadcastScheduleResponseDto
    {
        public Guid Id { get; set; }
        public Guid BroadcastId { get; set; }
        public DateTimeOffset StartDate { get; set; }
        public DateTimeOffset EndDate { get; set; }
        public string RepeatType { get; set; } = string.Empty;
        public string? RepeatDays { get; set; }
        public bool IsLocked { get; set; }

        public List<BroadcastTimeSlotResponseDto> TimeSlots { get; set; } = new List<BroadcastTimeSlotResponseDto>();
        public List<BroadcastDeviceResponseDto> Devices { get; set; } = new List<BroadcastDeviceResponseDto>();
    }

    // =========================================================
    // DTOs CHO KHUNG GIỜ PHÁT (TIME SLOT)
    // =========================================================

    public class CreateBroadcastTimeSlotDto
    {
        [Required(ErrorMessage = "Giờ bắt đầu là bắt buộc")]
        public TimeSpan StartTime { get; set; }

        [Required(ErrorMessage = "Giờ kết thúc là bắt buộc")]
        public TimeSpan EndTime { get; set; }

        public int DurationSeconds { get; set; } = 0;
    }

    public class BroadcastTimeSlotResponseDto
    {
        public Guid Id { get; set; }
        public Guid BroadcastScheduleId { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public int DurationSeconds { get; set; }
    }

    // =========================================================
    // DTOs CHO LIÊN KẾT THIẾT BỊ (BROADCAST DEVICE)
    // =========================================================

    public class BroadcastDeviceResponseDto
    {
        public Guid Id { get; set; }
        public Guid BroadcastScheduleId { get; set; }
        public Guid DeviceId { get; set; }
        public string? DeviceName { get; set; }
        public string? DeviceCode { get; set; }
        public string DeviceStatus { get; set; } = string.Empty;
        public bool IsPreempted { get; set; }
        public Guid? PreemptedByBroadcastId { get; set; }
    }

    // =========================================================
    // DTOs CHO QUÉT TRÙNG LỊCH (CONFLICT DETECTION - ISSUE #15)
    // =========================================================

    public class CheckConflictRequestDto
    {
        public Guid OrganizationId { get; set; }
        public Guid FacilityId { get; set; }
        public List<Guid>? DeviceIds { get; set; }
        public DateTimeOffset StartDate { get; set; }
        public DateTimeOffset EndDate { get; set; }
        public List<CreateBroadcastTimeSlotDto> TimeSlots { get; set; } = new List<CreateBroadcastTimeSlotDto>();
        public Guid? ExcludeBroadcastId { get; set; }
    }

    public class ConflictCheckResultDto
    {
        public bool HasConflict { get; set; }
        /// <summary>"Phát theo cơ sở" | "Phát theo thiết bị"</summary>
        public string BroadcastMode { get; set; } = "Phát theo cơ sở";
        public string? FacilityName { get; set; }
        public string SummaryMessage { get; set; } = string.Empty;
        public int TotalConflictingBroadcasts { get; set; }
        public List<ConflictingBroadcastItemDto> ConflictingBroadcasts { get; set; } = new List<ConflictingBroadcastItemDto>();
    }

    public class ConflictingBroadcastItemDto
    {
        public Guid BroadcastId { get; set; }
        public string BroadcastCode { get; set; } = string.Empty;
        public string BroadcastName { get; set; } = string.Empty;
        public string BroadcastType { get; set; } = string.Empty;
        public int Priority { get; set; }
        public Guid DeviceId { get; set; }
        public string? DeviceName { get; set; }
        public string? DeviceCode { get; set; }
        public string? FacilityName { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public DateTimeOffset ConflictStartDate { get; set; }
        public DateTimeOffset ConflictEndDate { get; set; }
        public int ConflictDurationMinutes { get; set; }
    }
}

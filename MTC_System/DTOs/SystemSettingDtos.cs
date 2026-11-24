using System;
using System.ComponentModel.DataAnnotations;

namespace MTC_System.DTOs
{
    public class SystemSettingResponseDto
    {
        public Guid Id { get; set; }
        public Guid OrganizationId { get; set; }
        public string SettingKey { get; set; } = string.Empty;
        public string SettingValue { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    /// <summary>
    /// [USR-SET-01] Thiết lập hệ thống cấp Tổ chức cao nhất (Root Organization Level).
    /// Áp dụng cho toàn bộ tổ chức và các cơ sở trực thuộc.
    /// </summary>
    public class OrganizationBroadcastSettingDto
    {
        public Guid OrganizationId { get; set; }
        public string? OrganizationName { get; set; }

        /// <summary>
        /// Chế độ tạo bản tin: "Phát theo cơ sở" hoặc "Phát theo thiết bị"
        /// </summary>
        [Required(ErrorMessage = "Chế độ tạo bản tin là bắt buộc")]
        public string BroadcastMode { get; set; } = "Phát theo cơ sở";

        /// <summary>
        /// Múi giờ tổ chức (VD: "SE Asia Standard Time" hoặc "Asia/Ho_Chi_Minh")
        /// </summary>
        [Required(ErrorMessage = "Múi giờ là bắt buộc")]
        public string Timezone { get; set; } = "SE Asia Standard Time";

        /// <summary>
        /// Cờ xác nhận chỉ hiển thị ở cấp Tổ chức cao nhất
        /// </summary>
        public bool IsRootOrganizationLevel { get; set; } = true;

        /// <summary>
        /// Quyền chỉnh sửa (chỉ cấp Tổ chức cao nhất mới có quyền)
        /// </summary>
        public bool CanEdit { get; set; } = true;
    }

    /// <summary>
    /// [USR-SET-02] Cấu hình cá nhân người dùng: Múi giờ và Chế độ xem bản tin (dạng bảng/lịch)
    /// </summary>
    public class UserPersonalSettingDto
    {
        public Guid UserId { get; set; }
        public string? Username { get; set; }

        /// <summary>
        /// Múi giờ cá nhân người dùng (mặc định "SE Asia Standard Time" - UTC+07:00)
        /// </summary>
        [Required(ErrorMessage = "Múi giờ cá nhân là bắt buộc")]
        public string Timezone { get; set; } = "SE Asia Standard Time";

        /// <summary>
        /// Chế độ xem bản tin: "Table" (Dạng bảng) | "Calendar" (Dạng lịch)
        /// Chấp nhận cả giá trị tiếng Việt: "Bảng", "Lịch", "Dạng bảng", "Dạng lịch"
        /// </summary>
        [Required(ErrorMessage = "Chế độ xem bản tin là bắt buộc")]
        public string BroadcastViewMode { get; set; } = "Table";

        /// <summary>
        /// Tên hiển thị tiếng Việt của chế độ xem: "Dạng bảng" | "Dạng lịch"
        /// </summary>
        public string BroadcastViewModeDisplay
        {
            get
            {
                if (string.Equals(BroadcastViewMode, "Calendar", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(BroadcastViewMode, "Lịch", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(BroadcastViewMode, "Dạng lịch", StringComparison.OrdinalIgnoreCase))
                {
                    return "Dạng lịch";
                }
                return "Dạng bảng";
            }
        }
    }

    /// <summary>
    /// Yêu cầu cập nhật cấu hình cá nhân
    /// </summary>
    public class UpdatePersonalSettingRequestDto
    {
        /// <summary>Múi giờ cá nhân người dùng</summary>
        public string? Timezone { get; set; }

        /// <summary>Chế độ xem bản tin: "Table" (Dạng bảng) hoặc "Calendar" (Dạng lịch)</summary>
        public string? BroadcastViewMode { get; set; }
    }

    /// <summary>
    /// Thông tin múi giờ hỗ trợ
    /// </summary>
    public class TimezoneItemDto
    {
        public string Id { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string UtcOffset { get; set; } = string.Empty;
    }
}

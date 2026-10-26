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

    public class OrganizationBroadcastSettingDto
    {
        [Required(ErrorMessage = "Chế độ phát là bắt buộc")]
        /// <summary>"Phát theo cơ sở" | "Phát theo thiết bị"</summary>
        public string BroadcastMode { get; set; } = "Phát theo cơ sở";

        /// <summary>Múi giờ tổ chức (VD: "SE Asia Standard Time" hoặc "UTC+07:00")</summary>
        [Required(ErrorMessage = "Múi giờ là bắt buộc")]
        public string Timezone { get; set; } = "SE Asia Standard Time";
    }

    public class UserPersonalSettingDto
    {
        public Guid UserId { get; set; }

        /// <summary>Múi giờ cá nhân người dùng</summary>
        public string Timezone { get; set; } = "SE Asia Standard Time";

        /// <summary>Chế độ xem bản tin: "Table" (dạng bảng) | "Calendar" (dạng lịch)</summary>
        public string BroadcastViewMode { get; set; } = "Table";
    }
}

using System;
using MTC_System.Common;

namespace MTC_System.Models
{
    /// <summary>
    /// [Dev 5] Bảng SystemNotifications (Thông báo hệ thống)
    /// OrganizationId? & FacilityId? = Nullable để lưu được cả System-level (NTF-12..14) lẫn End-User level (NTF-01..11)
    /// </summary>
    public class SystemNotification : BaseEntity
    {
        public Guid? OrganizationId { get; set; }
        public Guid? FacilityId { get; set; }

        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;

        /// <summary>Mã loại thông báo theo SRS: NTF-01 đến NTF-14</summary>
        public string NotificationType { get; set; } = string.Empty;

        /// <summary>JSON chứa danh sách OrganizationId nhận (phục vụ gửi thông báo đa địa bàn - UI trang 4)</summary>
        public string? TargetOrganizationIds { get; set; }

        /// <summary>Người nhận cụ thể nếu là thông báo cá nhân</summary>
        public Guid? TargetUserId { get; set; }

        public bool IsSent { get; set; } = false;

        /// <summary>Thời điểm hẹn gửi (null = gửi ngay)</summary>
        public DateTimeOffset? ScheduledAt { get; set; }
        public DateTimeOffset? SentAt { get; set; }
    }
}

using System;
using MTC_System.Common;

namespace MTC_System.Models
{
    /// <summary>
    /// [Dev 5] Bảng SystemNotificationReads (Trạng thái đã đọc thông báo của User)
    /// </summary>
    public class SystemNotificationRead : BaseEntity
    {
        public Guid NotificationId { get; set; }
        public Guid UserId { get; set; }
        public DateTimeOffset ReadAt { get; set; } = DateTimeOffset.UtcNow;
    }
}

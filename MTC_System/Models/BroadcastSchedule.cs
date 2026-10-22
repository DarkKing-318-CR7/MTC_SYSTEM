using System;
using System.Text.Json.Serialization;
using MTC_System.Common;

namespace MTC_System.Models
{
    /// <summary>
    /// [Dev 3] Bảng BroadcastSchedules (Lịch phát của bản tin)
    /// 1 Broadcast có nhiều Schedule, mỗi Schedule là 1 ngày phát với nhiều khung giờ
    /// </summary>
    public class BroadcastSchedule : BaseEntity
    {
        public Guid BroadcastId { get; set; }

        public DateTimeOffset StartDate { get; set; }
        public DateTimeOffset EndDate { get; set; }

        /// <summary>Chu kỳ lặp: "Không lặp" | "Hàng ngày" | "Theo tuần" | "Theo tháng"</summary>
        public string RepeatType { get; set; } = "Không lặp";

        /// <summary>
        /// Các thứ lặp lại khi RepeatType = "Theo tuần": "2,3,4,5,6,7,CN"
        /// Các ngày lặp lại khi RepeatType = "Theo tháng": "1,15,28"
        /// </summary>
        public string? RepeatDays { get; set; }

        /// <summary>Khóa chặn chỉnh sửa nếu bản tin đang ở trạng thái "Đang phát"</summary>
        public bool IsLocked { get; set; } = false;

        [JsonIgnore]
        public virtual ICollection<BroadcastTimeSlot> TimeSlots { get; set; } = new List<BroadcastTimeSlot>();

        [JsonIgnore]
        public virtual ICollection<BroadcastDevice> BroadcastDevices { get; set; } = new List<BroadcastDevice>();
    }
}

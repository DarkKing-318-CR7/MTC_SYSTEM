using System;
using MTC_System.Common;

namespace MTC_System.Models
{
    /// <summary>
    /// [Dev 3] Bảng BroadcastTimeSlots (Khung giờ phát trong ngày)
    /// 1 Schedule có nhiều TimeSlot (nhiều khung giờ trong ngày)
    /// </summary>
    public class BroadcastTimeSlot : BaseEntity
    {
        public Guid BroadcastScheduleId { get; set; }

        /// <summary>Giờ bắt đầu phát trong ngày (VD: 07:00:00)</summary>
        public TimeSpan StartTime { get; set; }

        /// <summary>Giờ kết thúc phát trong ngày (VD: 07:30:00)</summary>
        public TimeSpan EndTime { get; set; }

        /// <summary>
        /// Công thức kiểm tra trùng lịch (Conflict Engine - Issue #14):
        /// Trùng lịch IFF (NewStart < ExistingEnd) AND (NewEnd > ExistingStart)
        /// </summary>
        public int DurationSeconds { get; set; } = 0; // Tổng thời lượng khung giờ (giây)
    }
}

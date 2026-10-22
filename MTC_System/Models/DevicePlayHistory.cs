using System;
using MTC_System.Common;

namespace MTC_System.Models
{
    /// <summary>
    /// [Dev 4] Bảng DevicePlayHistories (Lịch sử phát bản tin của thiết bị)
    /// </summary>
    public class DevicePlayHistory : BaseEntity
    {
        public Guid DeviceId { get; set; }
        public Guid BroadcastId { get; set; }
        public Guid? BroadcastScheduleId { get; set; }

        public DateTimeOffset StartedAt { get; set; }
        public DateTimeOffset? EndedAt { get; set; }

        /// <summary>Kết quả: "Hoàn thành" | "Bị gián đoạn" | "Bị ghi đè"</summary>
        public string PlayResult { get; set; } = "Hoàn thành";
        public string? Notes { get; set; }
    }
}

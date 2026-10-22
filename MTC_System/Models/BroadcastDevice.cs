using System;
using MTC_System.Common;

namespace MTC_System.Models
{
    /// <summary>
    /// [Dev 3] Bảng BroadcastDevices (Liên kết bản tin với thiết bị nhận phát)
    /// 1 Schedule được phát trên nhiều Device
    /// </summary>
    public class BroadcastDevice : BaseEntity
    {
        public Guid BroadcastScheduleId { get; set; }
        public Guid DeviceId { get; set; }

        /// <summary>Trạng thái nhận: "Chờ phát" | "Đang phát" | "Tạm ngưng" | "Đã kết thúc"</summary>
        public string DeviceStatus { get; set; } = "Chờ phát";

        /// <summary>Bản tin này có bị ghi đè bởi bản tin khẩn cấp không</summary>
        public bool IsPreempted { get; set; } = false;
        public Guid? PreemptedByBroadcastId { get; set; }
    }
}

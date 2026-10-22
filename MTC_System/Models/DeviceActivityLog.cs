using System;
using MTC_System.Common;

namespace MTC_System.Models
{
    /// <summary>
    /// [Dev 4] Bảng DeviceActivityLogs (Lịch sử thao tác điều khiển thiết bị)
    /// Ghi lại mọi lệnh: Reboot, Volume, Kích hoạt, Tắt/Bật...
    /// </summary>
    public class DeviceActivityLog : BaseEntity
    {
        public Guid DeviceId { get; set; }
        public Guid? UserId { get; set; }     // Ai thực hiện (null nếu hệ thống tự chạy)

        /// <summary>Loại thao tác: "Reboot" | "Volume" | "Activate" | "Deactivate" | "Screenshot"</summary>
        public string ActionType { get; set; } = string.Empty;

        public string? ActionDetail { get; set; }  // JSON chi tiết tham số (volume=75, ...)
        public string? ErrorCodeRef { get; set; }  // Mã lỗi liên quan (MTT-001...)
        public string? IpAddress { get; set; }

        /// <summary>Kết quả: "Thành công" | "Thất bại"</summary>
        public string Result { get; set; } = "Thành công";
        public string? ErrorMessage { get; set; }
    }
}

using System;
using MTC_System.Common;

namespace MTC_System.Models
{
    /// <summary>
    /// [Dev 3] Bảng EmergencyBroadcasts (Bản tin khẩn cấp ưu tiên tuyệt đối)
    /// Khi kích hoạt: Tự động tạm dừng tất cả bản tin thường đang phát trên cùng thiết bị
    /// </summary>
    public class EmergencyBroadcast : BaseEntity
    {
        public Guid BroadcastId { get; set; }     // Bản tin khẩn cấp liên kết
        public Guid FacilityId { get; set; }      // Cơ sở phát khẩn cấp

        /// <summary>
        /// Phạm vi phát: "Tất cả thiết bị" | "Nhóm thiết bị" | "Thiết bị cụ thể"
        /// </summary>
        public string BroadcastScope { get; set; } = "Tất cả thiết bị";

        /// <summary>JSON chứa danh sách DeviceGroupId hoặc DeviceId được chọn phát</summary>
        public string? TargetIdsJson { get; set; }

        public DateTimeOffset ActivatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? EndedAt { get; set; }
        public Guid ActivatedBy { get; set; }

        /// <summary>Trạng thái: "Đang phát" | "Đã kết thúc"</summary>
        public string Status { get; set; } = "Đang phát";
    }
}

using System;
using System.Text.Json.Serialization;
using MTC_System.Common;

namespace MTC_System.Models
{
    /// <summary>
    /// [Dev 3] Bảng Broadcasts (Bản tin phát thanh / truyền hình)
    /// </summary>
    public class Broadcast : FacilityScopedEntity
    {
        public string BroadcastCode { get; set; } = string.Empty;
        public string BroadcastName { get; set; } = string.Empty;
        public string? Description { get; set; }

        /// <summary>Loại bản tin: "Thường" | "Khẩn cấp"</summary>
        public string BroadcastType { get; set; } = "Thường";

        /// <summary>Độ ưu tiên: 1 = Khẩn cấp (đè lịch thường), 2 = Thường</summary>
        public int Priority { get; set; } = 2;

        /// <summary>
        /// 9 trạng thái theo SRS:
        /// "Chờ duyệt" | "Từ chối duyệt" | "Chưa phát" | "Sắp tới lịch" |
        /// "Đang phát" | "Tạm ngưng" | "Chờ phát" | "Đã kết thúc" | "Đã hủy"
        /// </summary>
        public string Status { get; set; } = "Chờ duyệt";

        public string? RejectionReason { get; set; }

        /// <summary>Nguồn nội dung: Media file, Playlist, URL, Stream, FM</summary>
        public string ContentType { get; set; } = string.Empty;

        /// <summary>Khóa ngoại tùy chọn - điền MediaId hoặc PlaylistId, không điền cả 2</summary>
        public Guid? MediaId { get; set; }
        public Guid? PlaylistId { get; set; }

        public DateTimeOffset? ApprovedAt { get; set; }
        public Guid? ApprovedBy { get; set; }

        [JsonIgnore]
        public virtual ICollection<BroadcastSchedule> Schedules { get; set; } = new List<BroadcastSchedule>();
    }
}

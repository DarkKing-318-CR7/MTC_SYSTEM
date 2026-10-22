using System;
using MTC_System.Common;

namespace MTC_System.Models
{
    /// <summary>
    /// [Dev 2] Bảng SharedMedia (Lịch sử chia sẻ Media từ cơ sở cha xuống cơ sở con)
    /// Kế thừa BaseEntity vì có 2 FacilityId tham gia
    /// </summary>
    public class SharedMedia : BaseEntity
    {
        public Guid OrganizationId { get; set; }
        public Guid FromFacilityId { get; set; }   // Cơ sở cha đem chia sẻ
        public Guid ToFacilityId { get; set; }     // Cơ sở con nhận chia sẻ
        public Guid MediaId { get; set; }

        /// <summary>Trạng thái: "Chờ tiếp nhận" | "Đã tiếp nhận" | "Từ chối"</summary>
        public string Status { get; set; } = "Chờ tiếp nhận";

        public DateTimeOffset SharedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? RespondedAt { get; set; }
        public Guid? RespondedBy { get; set; }
    }
}

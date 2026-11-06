using System;
using System.Text.Json.Serialization;
using MTC_System.Common;

namespace MTC_System.Models
{
    /// <summary>
    /// [Dev 2] Bảng Media (Kho tệp tin đa phương tiện)
    /// Kế thừa FacilityScopedEntity (OrganizationId, FacilityId, Id, CreatedAt, IsDeleted)
    /// </summary>
    public class Media : FacilityScopedEntity
    {
        public string MediaName { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;

        /// <summary>Loại nội dung: Image, Audio, Video, LiveStream hoặc Frequency.</summary>
        public string FileType { get; set; } = string.Empty;

        /// <summary>Phương thức tạo nguồn, độc lập với loại nội dung media.</summary>
        public MediaSourceMethod SourceType { get; set; } = MediaSourceMethod.File;

        public long FileSize { get; set; } = 0;           // Dung lượng thực tế (bytes)
        public long SizeInBytes { get; set; } = 0;        // Phục vụ báo cáo dung lượng SYS-RPT-04
        public int DurationSeconds { get; set; } = 0;     // Thời lượng phát (giây)

        public string? ChecksumMd5 { get; set; }          // MD5/SHA256 để thiết bị đối chiếu (MTT-FILE-005)
        public string? ChecksumSha256 { get; set; }

        /// <summary>
        /// 4 trạng thái duyệt bắt buộc theo SRS:
        /// "Chờ duyệt" | "Đã duyệt" | "Từ chối duyệt" | "Đang xử lý"
        /// </summary>
        public string ApprovalStatus { get; set; } = "Chờ duyệt";

        public string? RejectionReason { get; set; }
        public DateTime? RejectedAt { get; set; }         // Dev 5 dùng để auto-xóa sau 24h (USR-MED-08)
        public Guid? RejectedBy { get; set; }

        /// <summary>true nếu media được cơ sở cha chia sẻ xuống (hiển thị icon chia sẻ ở UI)</summary>
        public bool IsSharedFromParent { get; set; } = false;

        public string? Url { get; set; }                  // External playback URL for a live stream
        public string? SourceUrl { get; set; }             // Original URL used to download a finite media file
        public decimal? FrequencyHz { get; set; }          // Numeric frequency metadata

        // Khóa ngoại sang danh mục lĩnh vực
        public Guid? CategoryId { get; set; }

        [JsonIgnore]
        public virtual MediaCategory? Category { get; set; }
    }
}

using System;
using MTC_System.Common;

namespace MTC_System.Models
{
    /// <summary>
    /// [Dev 5] Bảng AuditLogs (Nhật ký biến động dữ liệu toàn hệ thống)
    /// OrganizationId? & FacilityId? Nullable để lưu được cả System User lẫn End User
    /// Chỉ ghi thêm (Append-only), không có API Sửa/Xóa cho User thường.
    /// </summary>
    public class AuditLog : BaseEntity
    {
        public Guid? OrganizationId { get; set; }
        public Guid? FacilityId { get; set; }
        public Guid? UserId { get; set; }

        /// <summary>Loại thao tác: CREATE | UPDATE | DELETE | LOGIN | LOGOUT | APPROVE | REJECT...</summary>
        public string Action { get; set; } = string.Empty;

        /// <summary>Bảng/Thực thể bị tác động (tên class hoặc tên bảng DB)</summary>
        public string EntityName { get; set; } = string.Empty;

        public string? EntityId { get; set; }                 // Id bản ghi bị tác động
        public string? OldValueJson { get; set; }             // Giá trị cũ (JSON)
        public string? NewValueJson { get; set; }             // Giá trị mới (JSON)

        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }

        /// <summary>Dung lượng log (bytes) - phục vụ báo cáo dung lượng SYS-RPT-04</summary>
        public long SizeInBytes { get; set; } = 0;
    }
}

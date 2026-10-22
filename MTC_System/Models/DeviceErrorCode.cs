using MTC_System.Common;

namespace MTC_System.Models
{
    /// <summary>
    /// [Dev 4] Bảng DeviceErrorCodes (Danh mục 8 mã lỗi kỹ thuật chuẩn - MTT-001 đến MTT-008)
    /// Bảng tra cứu cố định, không phân biệt theo tổ chức
    /// </summary>
    public class DeviceErrorCode : BaseEntity
    {
        public string ErrorCode { get; set; } = string.Empty;   // MTT-001, MTT-002...
        public string ErrorName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Severity { get; set; } = "Trung bình";    // Thấp | Trung bình | Nghiêm trọng
    }
}

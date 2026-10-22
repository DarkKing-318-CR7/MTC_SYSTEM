using MTC_System.Common;

namespace MTC_System.Models
{
    /// <summary>
    /// [Dev 5] Bảng Packages (Danh mục gói cước bán sẵn)
    /// </summary>
    public class Package : BaseEntity
    {
        public string PackageCode { get; set; } = string.Empty;
        public string PackageName { get; set; } = string.Empty;
        public string? Description { get; set; }

        public decimal PricePerMonth { get; set; } = 0;    // Giá theo tháng (VNĐ)
        public int MaxDevices { get; set; } = 0;            // Số lượng thiết bị tối đa
        public int MaxStorageGB { get; set; } = 0;          // Dung lượng lưu trữ tối đa (GB)

        /// <summary>Kỳ hạn: 1 | 6 | 12 (tháng)</summary>
        public int DurationMonths { get; set; } = 1;

        public bool IsActive { get; set; } = true;
        public bool IsCustom { get; set; } = false;         // true = "Gói khác" nhập tay
    }
}

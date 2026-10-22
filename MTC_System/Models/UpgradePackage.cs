using MTC_System.Common;

namespace MTC_System.Models
{
    /// <summary>
    /// [Dev 5] Bảng UpgradePackages (Gói nâng cấp - bổ sung thêm tài nguyên)
    /// </summary>
    public class UpgradePackage : BaseEntity
    {
        public Guid PackageId { get; set; }                   // Gói cơ sở được nâng cấp từ
        public string UpgradeName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int AdditionalDevices { get; set; } = 0;       // Số thiết bị thêm
        public int AdditionalStorageGB { get; set; } = 0;     // Dung lượng thêm (GB)
        public decimal Price { get; set; } = 0;               // Giá gói nâng cấp (VNĐ)
        public bool IsActive { get; set; } = true;
    }
}

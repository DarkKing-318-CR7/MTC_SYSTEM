using MTC_System.Common;

namespace MTC_System.Models
{
    /// <summary>
    /// [Dev 4] Bảng DeviceVendors (Nhà phân phối / cung cấp thiết bị)
    /// OrganizationId? = null: Nhà cung cấp mặc định toàn hệ thống (Alta Media - được seed)
    /// OrganizationId? != null: Nhà cung cấp do riêng tổ chức tự thêm (USR-SET)
    /// </summary>
    public class DeviceVendor : BaseEntity
    {
        public Guid? OrganizationId { get; set; }
        public string VendorName { get; set; } = string.Empty;
        public string? ContactEmail { get; set; }
        public string? ContactPhone { get; set; }
        public string? Website { get; set; }
        public string? Address { get; set; }
        public bool IsDefault { get; set; } = false;  // true = nhà cung cấp mặc định hệ thống
    }
}

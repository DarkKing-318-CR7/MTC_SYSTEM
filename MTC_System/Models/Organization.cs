using System;
using MTC_System.Common;

namespace MTC_System.Models
{
    /// <summary>
    /// [Dev 1] Bảng Organizations (Khách hàng thuê bao SaaS cấp cao nhất)
    /// </summary>
    public class Organization : BaseEntity
    {
        public string OrganizationCode { get; set; } = string.Empty;
        public string OrganizationName { get; set; } = string.Empty;
        public string TaxCode { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string Status { get; set; } = "Còn hạn"; // Còn hạn, Hết hạn, Hủy bỏ
        public DateTimeOffset EffectiveDate { get; set; } = DateTimeOffset.UtcNow;
    }
}

using System;

namespace MTC_System.Common
{
    /// <summary>
    /// Kế thừa class này cho các bảng thuộc phạm vi Tổ chức (Organization):
    /// Facilities, SystemSettings, Subscriptions, Invoices.
    /// Có sẵn OrganizationId (Guid non-null) để phân lập dữ liệu giữa các khách hàng SaaS.
    /// </summary>
    public abstract class OrganizationScopedEntity : BaseEntity
    {
        public Guid OrganizationId { get; set; } // Tổ chức / Khách hàng thuê bao cấp cao nhất
    }
}

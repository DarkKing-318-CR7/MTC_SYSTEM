using System;

namespace MTC_System.Common
{
    /// <summary>
    /// Kế thừa class này cho 5 bảng gốc của Cơ sở (Media, Templates, Playlists, Broadcasts, Devices - và DeviceGroups).
    /// Tự động kế thừa OrganizationId từ OrganizationScopedEntity và bổ sung thêm FacilityId.
    /// Giúp Middleware/Global Filter tự động phân quyền dữ liệu theo cây cơ sở cha - con.
    /// </summary>
    public abstract class FacilityScopedEntity : OrganizationScopedEntity
    {
        public Guid FacilityId { get; set; } // Cơ sở trực thuộc trực tiếp
    }
}

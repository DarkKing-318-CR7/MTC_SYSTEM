using System;
using MTC_System.Common;

namespace MTC_System.Models
{
    /// <summary>
    /// [Dev 1] Bảng Facilities (Cơ sở / Địa bàn theo cây phân cấp cha - con)
    /// Kế thừa OrganizationScopedEntity (chỉ có OrganizationId, KHÔNG lặp lại FacilityId)
    /// </summary>
    public class Facility : OrganizationScopedEntity
    {
        public string FacilityCode { get; set; } = string.Empty;
        public string FacilityName { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// ParentId = null nếu là Cơ sở gốc cao nhất (Root - vd: TP.HCM)
        /// ParentId = Id của cơ sở cha nếu là cơ sở cấp con (vd: Quận/Huyện -> Phường/Xã)
        /// </summary>
        public Guid? ParentId { get; set; }
    }
}

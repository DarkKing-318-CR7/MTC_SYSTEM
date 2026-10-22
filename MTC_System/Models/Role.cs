using System;
using MTC_System.Common;

namespace MTC_System.Models
{
    /// <summary>
    /// [Dev 1] Bảng Roles (Vai trò: Administrator, Creator, IT, Approver)
    /// OrganizationId? = null: Vai trò mặc định toàn hệ thống
    /// OrganizationId? != null: Vai trò tùy biến do riêng Tổ chức đó tự tạo
    /// </summary>
    public class Role : BaseEntity
    {
        public Guid? OrganizationId { get; set; }
        public string RoleCode { get; set; } = string.Empty;
        public string RoleName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }
}

using System;
using MTC_System.Common;

namespace MTC_System.Models
{
    /// <summary>
    /// [Dev 1] Bảng RolePermissions (Ma trận phân quyền Role Matrix)
    /// </summary>
    public class RolePermission : BaseEntity
    {
        public Guid RoleId { get; set; }
        public Guid PermissionId { get; set; }
    }
}

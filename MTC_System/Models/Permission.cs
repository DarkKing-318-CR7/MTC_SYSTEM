using MTC_System.Common;

namespace MTC_System.Models
{
    /// <summary>
    /// [Dev 1] Bảng Permissions (Danh mục quyền chức năng)
    /// </summary>
    public class Permission : BaseEntity
    {
        public string PermissionCode { get; set; } = string.Empty;
        public string PermissionName { get; set; } = string.Empty;
        public string ModuleGroup { get; set; } = string.Empty; // Nhóm chức năng
    }
}

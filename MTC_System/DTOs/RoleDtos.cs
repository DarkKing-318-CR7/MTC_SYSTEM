using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MTC_System.DTOs
{
    public class RoleResponseDto
    {
        public Guid Id { get; set; }
        public string RoleCode { get; set; } = string.Empty;
        public string RoleName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public Guid? OrganizationId { get; set; }
        public bool IsSystemRole => !OrganizationId.HasValue;
        public int UserCount { get; set; }
    }

    public class RoleCreateDto
    {
        [Required(ErrorMessage = "Mã vai trò không được để trống")]
        [StringLength(50, ErrorMessage = "Mã vai trò tối đa 50 ký tự")]
        public string RoleCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Tên vai trò không được để trống")]
        [StringLength(100, ErrorMessage = "Tên vai trò tối đa 100 ký tự")]
        public string RoleName { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Mô tả tối đa 500 ký tự")]
        public string Description { get; set; } = string.Empty;

        public Guid? OrganizationId { get; set; }
    }

    public class PermissionResponseDto
    {
        public Guid Id { get; set; }
        public string PermissionCode { get; set; } = string.Empty;
        public string PermissionName { get; set; } = string.Empty;
        public string ModuleGroup { get; set; } = string.Empty;
        public bool IsAssigned { get; set; } = false;
    }

    public class RoleDetailResponseDto : RoleResponseDto
    {
        public List<PermissionResponseDto> Permissions { get; set; } = new();
    }

    public class AssignPermissionsDto
    {
        public List<Guid> PermissionIds { get; set; } = new();
    }
}

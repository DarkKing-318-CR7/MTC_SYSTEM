using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MTC_System.Common;
using MTC_System.DTOs;

namespace MTC_System.Services.Interfaces
{
    public interface IRoleService
    {
        Task<ApiResponse<List<RoleResponseDto>>> GetAllRolesAsync(Guid? organizationId = null);
        Task<ApiResponse<RoleDetailResponseDto>> GetRoleByIdAsync(Guid id);
        Task<ApiResponse<RoleResponseDto>> CreateRoleAsync(RoleCreateDto dto);
        Task<ApiResponse<List<PermissionResponseDto>>> GetAllPermissionsAsync();
        Task<ApiResponse<bool>> AssignPermissionsToRoleAsync(Guid roleId, AssignPermissionsDto dto);
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MTC_System.Common;
using MTC_System.Data;
using MTC_System.DTOs;
using MTC_System.Models;
using MTC_System.Services.Interfaces;

namespace MTC_System.Services
{
    public class RoleService : IRoleService
    {
        private readonly MyDbContext _context;

        public RoleService(MyDbContext context)
        {
            _context = context;
        }

        public async Task<ApiResponse<List<RoleResponseDto>>> GetAllRolesAsync(Guid? organizationId = null)
        {
            var query = _context.Roles.Where(r => !r.IsDeleted);

            if (organizationId.HasValue)
            {
                // Lấy vai trò hệ thống (null) HOẶC vai trò riêng của tổ chức đó
                query = query.Where(r => r.OrganizationId == null || r.OrganizationId == organizationId.Value);
            }

            var roles = await query.OrderBy(r => r.RoleCode).ToListAsync();
            var roleIds = roles.Select(r => r.Id).ToList();

            // Đếm số lượng user đang thuộc từng vai trò
            var userCounts = await _context.UserRoles
                .Where(ur => roleIds.Contains(ur.RoleId) && !ur.IsDeleted)
                .GroupBy(ur => ur.RoleId)
                .Select(g => new { RoleId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.RoleId, x => x.Count);

            var list = roles.Select(r => new RoleResponseDto
            {
                Id = r.Id,
                RoleCode = r.RoleCode,
                RoleName = r.RoleName,
                Description = r.Description,
                OrganizationId = r.OrganizationId,
                UserCount = userCounts.ContainsKey(r.Id) ? userCounts[r.Id] : 0
            }).ToList();

            return ApiResponse<List<RoleResponseDto>>.Ok(list);
        }

        public async Task<ApiResponse<RoleDetailResponseDto>> GetRoleByIdAsync(Guid id)
        {
            var role = await _context.Roles.FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted);
            if (role == null)
            {
                return ApiResponse<RoleDetailResponseDto>.Fail("Không tìm thấy vai trò.");
            }

            var userCount = await _context.UserRoles
                .CountAsync(ur => ur.RoleId == id && !ur.IsDeleted);

            var allPermissions = await _context.Permissions
                .Where(p => !p.IsDeleted)
                .OrderBy(p => p.ModuleGroup)
                .ThenBy(p => p.PermissionCode)
                .ToListAsync();

            var assignedPermIds = await _context.RolePermissions
                .Where(rp => rp.RoleId == id && !rp.IsDeleted)
                .Select(rp => rp.PermissionId)
                .ToListAsync();

            var permDtos = allPermissions.Select(p => new PermissionResponseDto
            {
                Id = p.Id,
                PermissionCode = p.PermissionCode,
                PermissionName = p.PermissionName,
                ModuleGroup = p.ModuleGroup,
                IsAssigned = assignedPermIds.Contains(p.Id)
            }).ToList();

            var detail = new RoleDetailResponseDto
            {
                Id = role.Id,
                RoleCode = role.RoleCode,
                RoleName = role.RoleName,
                Description = role.Description,
                OrganizationId = role.OrganizationId,
                UserCount = userCount,
                Permissions = permDtos
            };

            return ApiResponse<RoleDetailResponseDto>.Ok(detail);
        }

        public async Task<ApiResponse<RoleResponseDto>> CreateRoleAsync(RoleCreateDto dto)
        {
            string cleanCode = dto.RoleCode.Trim().ToUpper();
            bool exists = await _context.Roles.AnyAsync(r => r.RoleCode == cleanCode && !r.IsDeleted);
            if (exists)
            {
                return ApiResponse<RoleResponseDto>.Fail("Mã vai trò đã tồn tại.");
            }

            var role = new Role
            {
                RoleCode = cleanCode,
                RoleName = dto.RoleName.Trim(),
                Description = dto.Description?.Trim() ?? string.Empty,
                OrganizationId = dto.OrganizationId
            };

            _context.Roles.Add(role);
            await _context.SaveChangesAsync();

            var response = new RoleResponseDto
            {
                Id = role.Id,
                RoleCode = role.RoleCode,
                RoleName = role.RoleName,
                Description = role.Description,
                OrganizationId = role.OrganizationId,
                UserCount = 0
            };

            return ApiResponse<RoleResponseDto>.Ok(response, "Tạo vai trò thành công.");
        }

        public async Task<ApiResponse<List<PermissionResponseDto>>> GetAllPermissionsAsync()
        {
            var permissions = await _context.Permissions
                .Where(p => !p.IsDeleted)
                .OrderBy(p => p.ModuleGroup)
                .ThenBy(p => p.PermissionCode)
                .Select(p => new PermissionResponseDto
                {
                    Id = p.Id,
                    PermissionCode = p.PermissionCode,
                    PermissionName = p.PermissionName,
                    ModuleGroup = p.ModuleGroup,
                    IsAssigned = false
                })
                .ToListAsync();

            return ApiResponse<List<PermissionResponseDto>>.Ok(permissions);
        }

        public async Task<ApiResponse<bool>> AssignPermissionsToRoleAsync(Guid roleId, AssignPermissionsDto dto)
        {
            var role = await _context.Roles.FirstOrDefaultAsync(r => r.Id == roleId && !r.IsDeleted);
            if (role == null)
            {
                return ApiResponse<bool>.Fail("Không tìm thấy vai trò.");
            }

            // Xóa các phân quyền cũ
            var oldPerms = await _context.RolePermissions
                .Where(rp => rp.RoleId == roleId)
                .ToListAsync();

            _context.RolePermissions.RemoveRange(oldPerms);

            // Gán các quyền mới
            var validPermIds = await _context.Permissions
                .Where(p => dto.PermissionIds.Contains(p.Id) && !p.IsDeleted)
                .Select(p => p.Id)
                .ToListAsync();

            foreach (var permId in validPermIds)
            {
                _context.RolePermissions.Add(new RolePermission
                {
                    RoleId = roleId,
                    PermissionId = permId
                });
            }

            await _context.SaveChangesAsync();
            return ApiResponse<bool>.Ok(true, "Cập nhật ma trận phân quyền thành công.");
        }
    }
}

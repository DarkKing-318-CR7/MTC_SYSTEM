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
    public class UserService : IUserService
    {
        private readonly MyDbContext _context;

        public UserService(MyDbContext context)
        {
            _context = context;
        }

        public async Task<ApiResponse<PagedResult<UserResponseDto>>> GetPagedAsync(UserFilterDto filter)
        {
            var query = _context.Users
                .Where(u => !u.IsDeleted)
                .AsNoTracking();

            // 1. Tìm kiếm theo Username, FullName, Email
            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var search = filter.Search.Trim().ToLower();
                query = query.Where(u =>
                    u.Username.ToLower().Contains(search) ||
                    u.FullName.ToLower().Contains(search) ||
                    (u.Email != null && u.Email.ToLower().Contains(search)) ||
                    (u.PhoneNumber != null && u.PhoneNumber.Contains(search)));
            }

            // 2. Lọc theo Tổ chức
            if (filter.OrganizationId.HasValue)
            {
                query = query.Where(u => u.OrganizationId == filter.OrganizationId.Value);
            }

            // 3. Lọc theo Cơ sở
            if (filter.FacilityId.HasValue)
            {
                query = query.Where(u => u.FacilityId == filter.FacilityId.Value);
            }

            // 4. Lọc theo trạng thái hoạt động
            if (filter.IsActive.HasValue)
            {
                query = query.Where(u => u.IsActive == filter.IsActive.Value);
            }

            // 5. Lọc theo Vai trò
            if (filter.RoleId.HasValue)
            {
                var userIdsWithRole = _context.UserRoles
                    .Where(ur => ur.RoleId == filter.RoleId.Value && !ur.IsDeleted)
                    .Select(ur => ur.UserId);

                query = query.Where(u => userIdsWithRole.Contains(u.Id));
            }

            int totalCount = await query.CountAsync();
            int pageIndex = filter.Page > 0 ? filter.Page : 1;
            int pageSize = filter.PageSize > 0 ? filter.PageSize : 10;

            var rawUsers = await query
                .OrderByDescending(u => u.CreatedAt)
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var userIds = rawUsers.Select(u => u.Id).ToList();

            // Truy vấn lấy Role tương ứng của danh sách User
            var userRoles = await (from ur in _context.UserRoles
                                   join r in _context.Roles on ur.RoleId equals r.Id
                                   where userIds.Contains(ur.UserId) && !ur.IsDeleted && !r.IsDeleted
                                   select new
                                   {
                                       ur.UserId,
                                       RoleId = r.Id,
                                       RoleCode = r.RoleCode,
                                       RoleName = r.RoleName
                                   }).ToListAsync();

            // Truy vấn tên Tổ chức & Cơ sở
            var orgIds = rawUsers.Where(u => u.OrganizationId.HasValue).Select(u => u.OrganizationId!.Value).Distinct().ToList();
            var facilityIds = rawUsers.Where(u => u.FacilityId.HasValue).Select(u => u.FacilityId!.Value).Distinct().ToList();

            var orgs = await _context.Organizations
                .Where(o => orgIds.Contains(o.Id) && !o.IsDeleted)
                .ToDictionaryAsync(o => o.Id, o => o.OrganizationName);

            var facilities = await _context.Facilities
                .Where(f => facilityIds.Contains(f.Id) && !f.IsDeleted)
                .ToDictionaryAsync(f => f.Id, f => f.FacilityName);

            var items = rawUsers.Select(u =>
            {
                var roleInfo = userRoles.FirstOrDefault(ur => ur.UserId == u.Id);
                return new UserResponseDto
                {
                    Id = u.Id,
                    Username = u.Username,
                    FullName = u.FullName,
                    Email = u.Email,
                    PhoneNumber = u.PhoneNumber,
                    IsActive = u.IsActive,
                    TwoFactorEnabled = u.TwoFactorEnabled,
                    OrganizationId = u.OrganizationId,
                    OrganizationName = u.OrganizationId.HasValue && orgs.ContainsKey(u.OrganizationId.Value) ? orgs[u.OrganizationId.Value] : null,
                    FacilityId = u.FacilityId,
                    FacilityName = u.FacilityId.HasValue && facilities.ContainsKey(u.FacilityId.Value) ? facilities[u.FacilityId.Value] : null,
                    RoleId = roleInfo?.RoleId,
                    RoleCode = roleInfo?.RoleCode ?? (!u.OrganizationId.HasValue ? "ADMINISTRATOR" : "USER"),
                    RoleName = roleInfo?.RoleName ?? (!u.OrganizationId.HasValue ? "Quản trị viên" : "Người dùng"),
                    LastLoginAt = u.LastLoginAt,
                    LastActivityAt = u.LastActivityAt,
                    CreatedAt = u.CreatedAt
                };
            }).ToList();

            var pagedResult = PagedResult<UserResponseDto>.Create(items, totalCount, pageIndex, pageSize);
            return ApiResponse<PagedResult<UserResponseDto>>.Ok(pagedResult);
        }

        public async Task<ApiResponse<UserResponseDto>> GetByIdAsync(Guid id)
        {
            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted);

            if (user == null)
            {
                return ApiResponse<UserResponseDto>.Fail("Không tìm thấy người dùng.");
            }

            var roleInfo = await (from ur in _context.UserRoles
                                  join r in _context.Roles on ur.RoleId equals r.Id
                                  where ur.UserId == user.Id && !ur.IsDeleted && !r.IsDeleted
                                  select new { r.Id, r.RoleCode, r.RoleName })
                                  .FirstOrDefaultAsync();

            string? orgName = null;
            if (user.OrganizationId.HasValue)
            {
                var org = await _context.Organizations.FirstOrDefaultAsync(o => o.Id == user.OrganizationId.Value && !o.IsDeleted);
                orgName = org?.OrganizationName;
            }

            string? facilityName = null;
            if (user.FacilityId.HasValue)
            {
                var fac = await _context.Facilities.FirstOrDefaultAsync(f => f.Id == user.FacilityId.Value && !f.IsDeleted);
                facilityName = fac?.FacilityName;
            }

            var dto = new UserResponseDto
            {
                Id = user.Id,
                Username = user.Username,
                FullName = user.FullName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                IsActive = user.IsActive,
                TwoFactorEnabled = user.TwoFactorEnabled,
                OrganizationId = user.OrganizationId,
                OrganizationName = orgName,
                FacilityId = user.FacilityId,
                FacilityName = facilityName,
                RoleId = roleInfo?.Id,
                RoleCode = roleInfo?.RoleCode ?? (!user.OrganizationId.HasValue ? "ADMINISTRATOR" : "USER"),
                RoleName = roleInfo?.RoleName ?? (!user.OrganizationId.HasValue ? "Quản trị viên" : "Người dùng"),
                LastLoginAt = user.LastLoginAt,
                LastActivityAt = user.LastActivityAt,
                CreatedAt = user.CreatedAt
            };

            return ApiResponse<UserResponseDto>.Ok(dto);
        }

        public async Task<ApiResponse<UserResponseDto>> CreateAsync(UserCreateDto dto)
        {
            // 1. Kiểm tra Username duy nhất
            bool usernameExists = await _context.Users
                .AnyAsync(u => u.Username.ToLower() == dto.Username.Trim().ToLower() && !u.IsDeleted);
            if (usernameExists)
            {
                return ApiResponse<UserResponseDto>.Fail("Tên đăng nhập đã tồn tại trong hệ thống.");
            }

            // 2. Kiểm tra Email duy nhất (nếu có nhập)
            if (!string.IsNullOrWhiteSpace(dto.Email))
            {
                bool emailExists = await _context.Users
                    .AnyAsync(u => u.Email.ToLower() == dto.Email.Trim().ToLower() && !u.IsDeleted);
                if (emailExists)
                {
                    return ApiResponse<UserResponseDto>.Fail("Email đã được sử dụng bởi tài khoản khác.");
                }
            }

            // 3. Kiểm tra độ phức tạp của mật khẩu
            var passwordError = PasswordValidator.Validate(dto.Password);
            if (passwordError != null)
            {
                return ApiResponse<UserResponseDto>.Fail(passwordError);
            }

            // 4. Kiểm tra Role hợp lệ
            var role = await _context.Roles.FirstOrDefaultAsync(r => r.Id == dto.RoleId && !r.IsDeleted);
            if (role == null)
            {
                return ApiResponse<UserResponseDto>.Fail("Vai trò được chọn không tồn tại.");
            }

            // 5. Kiểm tra Tổ chức & Cơ sở (nếu được truyền)
            if (dto.OrganizationId.HasValue)
            {
                bool orgExists = await _context.Organizations.AnyAsync(o => o.Id == dto.OrganizationId.Value && !o.IsDeleted);
                if (!orgExists)
                {
                    return ApiResponse<UserResponseDto>.Fail("Tổ chức không tồn tại.");
                }
            }

            if (dto.FacilityId.HasValue)
            {
                var facility = await _context.Facilities.FirstOrDefaultAsync(f => f.Id == dto.FacilityId.Value && !f.IsDeleted);
                if (facility == null)
                {
                    return ApiResponse<UserResponseDto>.Fail("Cơ sở không tồn tại.");
                }
                if (dto.OrganizationId.HasValue && facility.OrganizationId != dto.OrganizationId.Value)
                {
                    return ApiResponse<UserResponseDto>.Fail("Cơ sở này không trực thuộc Tổ chức đã chọn.");
                }
            }

            // 6. Tạo người dùng mới và băm mật khẩu
            var newUser = new User
            {
                Username = dto.Username.Trim(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                FullName = dto.FullName.Trim(),
                Email = dto.Email?.Trim() ?? string.Empty,
                PhoneNumber = dto.PhoneNumber?.Trim() ?? string.Empty,
                OrganizationId = dto.OrganizationId,
                FacilityId = dto.FacilityId,
                IsActive = dto.IsActive,
                TwoFactorEnabled = false
            };

            _context.Users.Add(newUser);

            // Gán Role cho User
            _context.UserRoles.Add(new UserRole
            {
                UserId = newUser.Id,
                RoleId = role.Id
            });

            await _context.SaveChangesAsync();

            return await GetByIdAsync(newUser.Id);
        }

        public async Task<ApiResponse<UserResponseDto>> UpdateAsync(Guid id, UserUpdateDto dto)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted);
            if (user == null)
            {
                return ApiResponse<UserResponseDto>.Fail("Không tìm thấy người dùng.");
            }

            // 1. Kiểm tra Email không trùng lặp với người dùng khác
            if (!string.IsNullOrWhiteSpace(dto.Email))
            {
                bool emailExists = await _context.Users
                    .AnyAsync(u => u.Id != id && u.Email.ToLower() == dto.Email.Trim().ToLower() && !u.IsDeleted);
                if (emailExists)
                {
                    return ApiResponse<UserResponseDto>.Fail("Email đã được sử dụng bởi người dùng khác.");
                }
            }

            // 2. Kiểm tra Role hợp lệ (nếu có cập nhật)
            if (dto.RoleId.HasValue)
            {
                var roleExists = await _context.Roles.AnyAsync(r => r.Id == dto.RoleId.Value && !r.IsDeleted);
                if (!roleExists)
                {
                    return ApiResponse<UserResponseDto>.Fail("Vai trò không tồn tại.");
                }

                // Cập nhật UserRoles
                var currentRole = await _context.UserRoles.FirstOrDefaultAsync(ur => ur.UserId == id && !ur.IsDeleted);
                if (currentRole != null)
                {
                    currentRole.RoleId = dto.RoleId.Value;
                    currentRole.UpdatedAt = DateTimeOffset.UtcNow;
                }
                else
                {
                    _context.UserRoles.Add(new UserRole
                    {
                        UserId = id,
                        RoleId = dto.RoleId.Value
                    });
                }
            }

            // 3. Kiểm tra Cơ sở thuộc Tổ chức
            if (dto.FacilityId.HasValue && dto.OrganizationId.HasValue)
            {
                var facility = await _context.Facilities.FirstOrDefaultAsync(f => f.Id == dto.FacilityId.Value && !f.IsDeleted);
                if (facility != null && facility.OrganizationId != dto.OrganizationId.Value)
                {
                    return ApiResponse<UserResponseDto>.Fail("Cơ sở không trực thuộc Tổ chức đã chọn.");
                }
            }

            user.FullName = dto.FullName.Trim();
            user.Email = dto.Email?.Trim() ?? string.Empty;
            user.PhoneNumber = dto.PhoneNumber?.Trim() ?? string.Empty;
            user.OrganizationId = dto.OrganizationId;
            user.FacilityId = dto.FacilityId;
            user.IsActive = dto.IsActive;
            user.UpdatedAt = DateTimeOffset.UtcNow;

            await _context.SaveChangesAsync();

            return await GetByIdAsync(id);
        }

        public async Task<ApiResponse<bool>> UpdateStatusAsync(Guid id, bool isActive)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted);
            if (user == null)
            {
                return ApiResponse<bool>.Fail("Không tìm thấy người dùng.");
            }

            if (user.Username.Equals("admin", StringComparison.OrdinalIgnoreCase))
            {
                return ApiResponse<bool>.Fail("Không thể khóa tài khoản Quản trị viên tối cao của hệ thống.");
            }

            user.IsActive = isActive;
            user.UpdatedAt = DateTimeOffset.UtcNow;

            // Nếu khóa tài khoản, thu hồi toàn bộ token đăng nhập
            if (!isActive)
            {
                var activeTokens = await _context.RefreshTokens
                    .Where(r => r.UserId == id && !r.IsRevoked)
                    .ToListAsync();

                foreach (var token in activeTokens)
                {
                    token.IsRevoked = true;
                    token.UpdatedAt = DateTimeOffset.UtcNow;
                }
            }

            await _context.SaveChangesAsync();
            return ApiResponse<bool>.Ok(true, isActive ? "Mở khóa tài khoản thành công." : "Đã khóa tài khoản thành công.");
        }

        public async Task<ApiResponse<bool>> ResetPasswordAsync(Guid id, ResetPasswordDto dto)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted);
            if (user == null)
            {
                return ApiResponse<bool>.Fail("Không tìm thấy người dùng.");
            }

            var passwordError = PasswordValidator.Validate(dto.NewPassword);
            if (passwordError != null)
            {
                return ApiResponse<bool>.Fail(passwordError);
            }

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
            user.UpdatedAt = DateTimeOffset.UtcNow;

            // Thu hồi toàn bộ token cũ để bắt buộc đăng nhập lại
            var activeTokens = await _context.RefreshTokens
                .Where(r => r.UserId == id && !r.IsRevoked)
                .ToListAsync();

            foreach (var token in activeTokens)
            {
                token.IsRevoked = true;
                token.UpdatedAt = DateTimeOffset.UtcNow;
            }

            await _context.SaveChangesAsync();
            return ApiResponse<bool>.Ok(true, "Đặt lại mật khẩu thành công. Tất cả phiên đăng nhập cũ đã bị chấm dứt.");
        }

        public async Task<ApiResponse<bool>> DeleteAsync(Guid id)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted);
            if (user == null)
            {
                return ApiResponse<bool>.Fail("Không tìm thấy người dùng.");
            }

            if (user.Username.Equals("admin", StringComparison.OrdinalIgnoreCase))
            {
                return ApiResponse<bool>.Fail("Không thể xóa tài khoản Quản trị viên tối cao của hệ thống.");
            }

            // Xóa mềm
            user.IsDeleted = true;
            user.UpdatedAt = DateTimeOffset.UtcNow;

            // Thu hồi token
            var activeTokens = await _context.RefreshTokens
                .Where(r => r.UserId == id && !r.IsRevoked)
                .ToListAsync();

            foreach (var token in activeTokens)
            {
                token.IsRevoked = true;
                token.UpdatedAt = DateTimeOffset.UtcNow;
            }

            await _context.SaveChangesAsync();
            return ApiResponse<bool>.Ok(true, "Đã xóa người dùng thành công.");
        }
    }
}

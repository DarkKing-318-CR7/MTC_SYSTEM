using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using MTC_System.Common;
using MTC_System.Data;
using MTC_System.DTOs;
using MTC_System.Models;
using MTC_System.Services.Interfaces;

namespace MTC_System.Services
{
    public class AuthService : IAuthService
    {
        private readonly MyDbContext _context;
        private readonly IConfiguration _config;

        public AuthService(MyDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        public async Task<ApiResponse<LoginResponseDto>> LoginAsync(LoginRequestDto dto)
        {
            // 1. Tìm User trong Database (không phân biệt hoa thường, chưa bị xóa mềm)
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username.ToLower() == dto.Username.ToLower() && !u.IsDeleted);

            if (user == null)
            {
                return ApiResponse<LoginResponseDto>.Fail("Tên đăng nhập hoặc mật khẩu không chính xác.");
            }

            // 2. Kiểm tra tài khoản có bị khóa không
            if (!user.IsActive)
            {
                return ApiResponse<LoginResponseDto>.Fail("Tài khoản của bạn đã bị khóa hoặc ngừng hoạt động. Vui lòng liên hệ quản trị viên.");
            }

            // 3. Kiểm tra mật khẩu bằng BCrypt
            bool isPasswordValid = false;
            try
            {
                isPasswordValid = BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash);
            }
            catch
            {
                // Phòng trường hợp dữ liệu cũ seed mật khẩu chưa hash dạng BCrypt
                isPasswordValid = false;
            }

            if (!isPasswordValid)
            {
                return ApiResponse<LoginResponseDto>.Fail("Tên đăng nhập hoặc mật khẩu không chính xác.");
            }

            // 4. Lấy Role và thông tin Tổ chức / Cơ sở
            var (roleCode, orgName, facilityName) = await GetUserMetadataAsync(user);

            // 5. Kiểm tra 2FA (Nếu bật 2FA thì yêu cầu xác thực 2 bước)
            if (user.TwoFactorEnabled)
            {
                return ApiResponse<LoginResponseDto>.Ok(new LoginResponseDto
                {
                    Requires2FA = true,
                    User = MapUserAuthInfo(user, roleCode, orgName, facilityName)
                }, "Yêu cầu mã xác thực 2 bước (2FA).");
            }

            // 6. Cấp Access Token (JWT - 15 phút) & Refresh Token (7 ngày)
            int accessDurationMinutes = _config.GetValue<int>("Jwt:AccessTokenDurationMinutes", 15);
            int refreshDurationDays = _config.GetValue<int>("Jwt:RefreshTokenDurationDays", 7);

            var expiresAt = DateTime.UtcNow.AddMinutes(accessDurationMinutes);
            string accessToken = GenerateJwtToken(user, roleCode, accessDurationMinutes);
            string refreshTokenStr = GenerateRefreshTokenString();

            // 7. Lưu Refresh Token vào Database
            var refreshTokenEntity = new RefreshToken
            {
                UserId = user.Id,
                Token = refreshTokenStr,
                ExpiresAt = DateTime.UtcNow.AddDays(refreshDurationDays),
                IsRevoked = false
            };
            _context.RefreshTokens.Add(refreshTokenEntity);

            // 8. Cập nhật thời gian đăng nhập
            user.LastLoginAt = DateTimeOffset.UtcNow;
            user.LastActivityAt = DateTimeOffset.UtcNow;
            await _context.SaveChangesAsync();

            var response = new LoginResponseDto
            {
                AccessToken = accessToken,
                RefreshToken = refreshTokenStr,
                TokenType = "Bearer",
                ExpiresAt = expiresAt,
                Requires2FA = false,
                User = MapUserAuthInfo(user, roleCode, orgName, facilityName)
            };

            return ApiResponse<LoginResponseDto>.Ok(response, "Đăng nhập thành công.");
        }

        public async Task<ApiResponse<LoginResponseDto>> RefreshTokenAsync(RefreshTokenRequestDto dto)
        {
            // 1. Tìm Refresh Token trong Database
            var tokenRecord = await _context.RefreshTokens
                .FirstOrDefaultAsync(r => r.Token == dto.RefreshToken && !r.IsDeleted);

            if (tokenRecord == null || tokenRecord.IsRevoked)
            {
                return ApiResponse<LoginResponseDto>.Fail("Refresh Token không hợp lệ hoặc đã bị thu hồi.");
            }

            if (tokenRecord.ExpiresAt < DateTime.UtcNow)
            {
                return ApiResponse<LoginResponseDto>.Fail("Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.");
            }

            // 2. Tìm User
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == tokenRecord.UserId && !u.IsDeleted);
            if (user == null || !user.IsActive)
            {
                return ApiResponse<LoginResponseDto>.Fail("Tài khoản không tồn tại hoặc đã bị khóa.");
            }

            // 3. Thu hồi token cũ (Refresh Token Rotation - tăng cường bảo mật)
            tokenRecord.IsRevoked = true;
            tokenRecord.UpdatedAt = DateTimeOffset.UtcNow;

            // 4. Cấp Access Token & Refresh Token mới
            var (roleCode, orgName, facilityName) = await GetUserMetadataAsync(user);
            int accessDurationMinutes = _config.GetValue<int>("Jwt:AccessTokenDurationMinutes", 15);
            int refreshDurationDays = _config.GetValue<int>("Jwt:RefreshTokenDurationDays", 7);

            var expiresAt = DateTime.UtcNow.AddMinutes(accessDurationMinutes);
            string newAccessToken = GenerateJwtToken(user, roleCode, accessDurationMinutes);
            string newRefreshTokenStr = GenerateRefreshTokenString();

            _context.RefreshTokens.Add(new RefreshToken
            {
                UserId = user.Id,
                Token = newRefreshTokenStr,
                ExpiresAt = DateTime.UtcNow.AddDays(refreshDurationDays),
                IsRevoked = false
            });

            user.LastActivityAt = DateTimeOffset.UtcNow;
            await _context.SaveChangesAsync();

            var response = new LoginResponseDto
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshTokenStr,
                TokenType = "Bearer",
                ExpiresAt = expiresAt,
                Requires2FA = false,
                User = MapUserAuthInfo(user, roleCode, orgName, facilityName)
            };

            return ApiResponse<LoginResponseDto>.Ok(response, "Làm mới Token thành công.");
        }

        public async Task<ApiResponse<bool>> LogoutAsync(Guid userId, string token)
        {
            // Thu hồi token tương ứng
            var tokens = await _context.RefreshTokens
                .Where(r => r.UserId == userId && !r.IsRevoked)
                .ToListAsync();

            if (!string.IsNullOrEmpty(token))
            {
                var specificToken = tokens.FirstOrDefault(r => r.Token == token);
                if (specificToken != null)
                {
                    specificToken.IsRevoked = true;
                    specificToken.UpdatedAt = DateTimeOffset.UtcNow;
                }
            }
            else
            {
                // Thu hồi tất cả token của user nếu không chỉ định
                foreach (var t in tokens)
                {
                    t.IsRevoked = true;
                    t.UpdatedAt = DateTimeOffset.UtcNow;
                }
            }

            await _context.SaveChangesAsync();
            return ApiResponse<bool>.Ok(true, "Đăng xuất thành công.");
        }

        public async Task<ApiResponse<bool>> ChangePasswordAsync(Guid userId, ChangePasswordDto dto)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted);
            if (user == null)
            {
                return ApiResponse<bool>.Fail("Người dùng không tồn tại.");
            }

            // 1. Kiểm tra mật khẩu cũ
            if (!BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, user.PasswordHash))
            {
                return ApiResponse<bool>.Fail("Mật khẩu hiện tại không chính xác.");
            }

            // 2. Kiểm tra mật khẩu mới không được trùng mật khẩu cũ
            if (dto.CurrentPassword == dto.NewPassword)
            {
                return ApiResponse<bool>.Fail("Mật khẩu mới không được trùng với mật khẩu hiện tại.");
            }

            // 3. Kiểm tra độ phức tạp của mật khẩu mới theo chuẩn SRS (Tối thiểu 8 ký tự, hoa, thường, số, ký tự đặc biệt)
            var validationError = ValidatePasswordComplexity(dto.NewPassword);
            if (validationError != null)
            {
                return ApiResponse<bool>.Fail(validationError);
            }

            // 4. Băm mật khẩu mới bằng BCrypt và lưu lại
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
            user.UpdatedAt = DateTimeOffset.UtcNow;

            // Thu hồi tất cả refresh token cũ để bắt buộc đăng nhập lại
            var activeTokens = await _context.RefreshTokens
                .Where(r => r.UserId == userId && !r.IsRevoked)
                .ToListAsync();
            foreach (var t in activeTokens)
            {
                t.IsRevoked = true;
                t.UpdatedAt = DateTimeOffset.UtcNow;
            }

            await _context.SaveChangesAsync();
            return ApiResponse<bool>.Ok(true, "Đổi mật khẩu thành công. Vui lòng đăng nhập lại với mật khẩu mới.");
        }

        public async Task<ApiResponse<UserAuthInfoDto>> GetProfileAsync(Guid userId)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted);
            if (user == null)
            {
                return ApiResponse<UserAuthInfoDto>.Fail("Không tìm thấy thông tin người dùng.");
            }

            var (roleCode, orgName, facilityName) = await GetUserMetadataAsync(user);
            return ApiResponse<UserAuthInfoDto>.Ok(MapUserAuthInfo(user, roleCode, orgName, facilityName));
        }

        public async Task<ApiResponse<UserAuthInfoDto>> UpdateProfileAsync(Guid userId, UpdateProfileDto dto)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted);
            if (user == null)
            {
                return ApiResponse<UserAuthInfoDto>.Fail("Không tìm thấy người dùng.");
            }

            // Kiểm tra trùng email với người dùng khác
            if (!string.IsNullOrWhiteSpace(dto.Email) && dto.Email.ToLower() != user.Email.ToLower())
            {
                bool emailExists = await _context.Users.AnyAsync(u => u.Email.ToLower() == dto.Email.ToLower() && u.Id != userId && !u.IsDeleted);
                if (emailExists)
                {
                    return ApiResponse<UserAuthInfoDto>.Fail("Email này đã được sử dụng bởi tài khoản khác.");
                }
                user.Email = dto.Email.Trim();
            }

            user.FullName = dto.FullName.Trim();
            user.PhoneNumber = dto.PhoneNumber?.Trim() ?? string.Empty;
            user.UpdatedAt = DateTimeOffset.UtcNow;

            await _context.SaveChangesAsync();

            var (roleCode, orgName, facilityName) = await GetUserMetadataAsync(user);
            return ApiResponse<UserAuthInfoDto>.Ok(MapUserAuthInfo(user, roleCode, orgName, facilityName), "Cập nhật thông tin thành công.");
        }

        #region Private Helper Methods

        private string GenerateJwtToken(User user, string roleCode, int durationMinutes)
        {
            var secretKey = _config["Jwt:SecretKey"] ?? "DEFAULT_SECRET_KEY_MTC_SYSTEM_2026_@_LONG_ENOUGH_KEY";
            var issuer = _config["Jwt:Issuer"] ?? "MTC_Backend";
            var audience = _config["Jwt:Audience"] ?? "MTC_Clients";

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, roleCode),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            if (!string.IsNullOrEmpty(user.Email))
            {
                claims.Add(new Claim(ClaimTypes.Email, user.Email));
            }

            if (user.OrganizationId.HasValue)
            {
                claims.Add(new Claim("organizationId", user.OrganizationId.Value.ToString()));
            }

            if (user.FacilityId.HasValue)
            {
                claims.Add(new Claim("facilityId", user.FacilityId.Value.ToString()));
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(durationMinutes),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private string GenerateRefreshTokenString()
        {
            var randomNumber = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }

        private async Task<(string roleCode, string? orgName, string? facilityName)> GetUserMetadataAsync(User user)
        {
            // 1. Lấy vai trò (Role)
            string roleCode = "User";
            var userRole = await _context.UserRoles.FirstOrDefaultAsync(ur => ur.UserId == user.Id && !ur.IsDeleted);
            if (userRole != null)
            {
                var role = await _context.Roles.FirstOrDefaultAsync(r => r.Id == userRole.RoleId && !r.IsDeleted);
                if (role != null)
                {
                    roleCode = role.RoleCode;
                }
            }
            else if (!user.OrganizationId.HasValue && !user.FacilityId.HasValue)
            {
                roleCode = "Administrator"; // System Admin
            }

            // 2. Lấy tên tổ chức
            string? orgName = null;
            if (user.OrganizationId.HasValue)
            {
                var org = await _context.Organizations.FirstOrDefaultAsync(o => o.Id == user.OrganizationId.Value);
                orgName = org?.OrganizationName;
            }

            // 3. Lấy tên cơ sở
            string? facilityName = null;
            if (user.FacilityId.HasValue)
            {
                var fac = await _context.Facilities.FirstOrDefaultAsync(f => f.Id == user.FacilityId.Value);
                facilityName = fac?.FacilityName;
            }

            return (roleCode, orgName, facilityName);
        }

        private UserAuthInfoDto MapUserAuthInfo(User user, string roleCode, string? orgName, string? facilityName)
        {
            return new UserAuthInfoDto
            {
                UserId = user.Id,
                Username = user.Username,
                FullName = user.FullName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                Role = roleCode,
                OrganizationId = user.OrganizationId,
                OrganizationName = orgName,
                FacilityId = user.FacilityId,
                FacilityName = facilityName,
                IsSuperAdmin = !user.OrganizationId.HasValue
            };
        }

        private string? ValidatePasswordComplexity(string password)
        {
            if (string.IsNullOrWhiteSpace(password) || password.Length < 8)
            {
                return "Mật khẩu phải có độ dài tối thiểu 8 ký tự.";
            }

            if (!Regex.IsMatch(password, @"[A-Z]"))
            {
                return "Mật khẩu phải chứa ít nhất 1 chữ cái in hoa (A-Z).";
            }

            if (!Regex.IsMatch(password, @"[a-z]"))
            {
                return "Mật khẩu phải chứa ít nhất 1 chữ cái in thường (a-z).";
            }

            if (!Regex.IsMatch(password, @"[0-9]"))
            {
                return "Mật khẩu phải chứa ít nhất 1 chữ số (0-9).";
            }

            if (!Regex.IsMatch(password, @"[!@#$%^&*(),.?"":{}|<>\-_=+]"))
            {
                return "Mật khẩu phải chứa ít nhất 1 ký tự đặc biệt (!@#$%^&*...).";
            }

            return null;
        }

        #endregion
    }
}

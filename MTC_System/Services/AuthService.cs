using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
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
using OtpNet;
using QRCoder;

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
                if (user.TwoFactorEnabled && user.BackupCodesCount <= 0)
                {
                    return ApiResponse<LoginResponseDto>.Fail("Tài khoản đã bị tạm khóa bảo mật do đã sử dụng hết 10 mã dự phòng (SEC-2FA-LGN-06). Vui lòng liên hệ Quản trị viên để mở khóa.");
                }
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
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted);

            // Ràng buộc bảo mật SEC-2FA-LGN-06:
            // Tự động khóa tài khoản sau khi logout nếu đã dùng hết 10 mã dự phòng
            if (user != null && user.TwoFactorEnabled && user.BackupCodesCount <= 0)
            {
                user.IsActive = false; // Tạm khóa tài khoản
                user.UpdatedAt = DateTimeOffset.UtcNow;
            }

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

        #region 2FA Implementation (Issue #3 / SEC-2FA-LGN-06)
        public async Task<ApiResponse<TwoFactorSetupResponseDto>> SetupTwoFactorAsync(Guid userId)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted);
            if (user == null)
            {
                return ApiResponse<TwoFactorSetupResponseDto>.Fail("Người dùng không tồn tại.");
            }

            if (user.TwoFactorEnabled)
            {
                return ApiResponse<TwoFactorSetupResponseDto>.Fail("Tài khoản đã kích hoạt xác thực 2 bước (2FA) rồi.");
            }

            // 1. Sinh Secret Key 20 byte (160 bit) ngẫu nhiên theo chuẩn Base32
            byte[] secretBytes = KeyGeneration.GenerateRandomKey(20);
            string secretKeyBase32 = Base32Encoding.ToString(secretBytes);

            // Lưu tạm thời secret key vào DB (chưa bật TwoFactorEnabled cho đến khi verify mã đầu tiên)
            user.TwoFactorSecretKey = secretKeyBase32;
            user.UpdatedAt = DateTimeOffset.UtcNow;
            await _context.SaveChangesAsync();

            // 2. Tạo URL chuẩn otpauth://totp/...
            string issuer = "MTC_System";
            string otpAuthUrl = $"otpauth://totp/{issuer}:{Uri.EscapeDataString(user.Username)}?secret={secretKeyBase32}&issuer={issuer}&digits=6&period=30";

            // 3. Sinh ảnh QR Code dạng Data URL Base64 bằng QRCoder
            using var qrGenerator = new QRCodeGenerator();
            using var qrCodeData = qrGenerator.CreateQrCode(otpAuthUrl, QRCodeGenerator.ECCLevel.Q);
            using var qrCode = new PngByteQRCode(qrCodeData);
            byte[] qrCodeBytes = qrCode.GetGraphic(20);
            string qrCodeDataUrl = $"data:image/png;base64,{Convert.ToBase64String(qrCodeBytes)}";

            var result = new TwoFactorSetupResponseDto
            {
                SecretKey = secretKeyBase32,
                ManualEntryKey = FormatSecretKey(secretKeyBase32),
                QrCodeDataUrl = qrCodeDataUrl,
                OtpAuthUrl = otpAuthUrl
            };

            return ApiResponse<TwoFactorSetupResponseDto>.Ok(result, "Tạo thông tin cài đặt 2FA thành công. Hãy quét mã QR bằng Google Authenticator hoặc nhập mã thủ công.");
        }

        public async Task<ApiResponse<TwoFactorEnableResponseDto>> EnableTwoFactorAsync(Guid userId, EnableTwoFactorDto dto)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted);
            if (user == null)
            {
                return ApiResponse<TwoFactorEnableResponseDto>.Fail("Người dùng không tồn tại.");
            }

            if (user.TwoFactorEnabled)
            {
                return ApiResponse<TwoFactorEnableResponseDto>.Fail("Tài khoản đã kích hoạt 2FA trước đó.");
            }

            if (string.IsNullOrEmpty(user.TwoFactorSecretKey))
            {
                return ApiResponse<TwoFactorEnableResponseDto>.Fail("Vui lòng thực hiện bước thiết lập 2FA (Setup) trước khi kích hoạt.");
            }

            // 1. Kiểm tra mã 6 số người dùng nhập
            byte[] secretBytes = Base32Encoding.ToBytes(user.TwoFactorSecretKey);
            var totp = new Totp(secretBytes);
            bool isCodeValid = totp.VerifyTotp(dto.Code.Trim(), out _, new VerificationWindow(previous: 1, future: 1));

            if (!isCodeValid)
            {
                return ApiResponse<TwoFactorEnableResponseDto>.Fail("Mã xác thực 6 số không chính xác hoặc đã hết hạn.");
            }

            // 2. Mã hợp lệ -> Kích hoạt 2FA
            user.TwoFactorEnabled = true;

            // 3. Sinh 10 mã dự phòng (Backup Codes) dùng một lần
            var (backupItems, rawCodes) = Generate10BackupCodes();
            user.BackupCodesJson = JsonSerializer.Serialize(backupItems);
            user.BackupCodesCount = 10;
            user.UpdatedAt = DateTimeOffset.UtcNow;
            await _context.SaveChangesAsync();

            var response = new TwoFactorEnableResponseDto
            {
                IsSuccess = true,
                Message = "Kích hoạt xác thực 2 bước (2FA) thành công! Hãy lưu lại 10 mã dự phòng này ở nơi an toàn. Mỗi mã chỉ dùng được một lần.",
                BackupCodes = rawCodes
            };

            return ApiResponse<TwoFactorEnableResponseDto>.Ok(response, response.Message);
        }

        public async Task<ApiResponse<LoginResponseDto>> VerifyTwoFactorLoginAsync(VerifyTwoFactorLoginDto dto)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == dto.UserId && !u.IsDeleted);
            if (user == null)
            {
                return ApiResponse<LoginResponseDto>.Fail("Người dùng không tồn tại.");
            }

            if (!user.IsActive)
            {
                if (user.TwoFactorEnabled && user.BackupCodesCount <= 0)
                {
                    return ApiResponse<LoginResponseDto>.Fail("Tài khoản đã bị tạm khóa bảo mật do đã sử dụng hết 10 mã dự phòng (SEC-2FA-LGN-06). Vui lòng liên hệ Quản trị viên để mở khóa.");
                }
                return ApiResponse<LoginResponseDto>.Fail("Tài khoản đã bị vô hiệu hóa.");
            }

            if (!user.TwoFactorEnabled || string.IsNullOrEmpty(user.TwoFactorSecretKey))
            {
                return ApiResponse<LoginResponseDto>.Fail("Tài khoản này chưa kích hoạt tính năng xác thực 2 bước.");
            }

            string cleanCode = dto.Code.Trim().Replace(" ", "");
            bool isTotpValid = false;
            bool isBackupCodeValid = false;

            // 1. Thử xác thực với mã TOTP 6 số
            if (cleanCode.Length == 6 && int.TryParse(cleanCode, out _))
            {
                byte[] secretBytes = Base32Encoding.ToBytes(user.TwoFactorSecretKey);
                var totp = new Totp(secretBytes);
                isTotpValid = totp.VerifyTotp(cleanCode, out _, new VerificationWindow(previous: 1, future: 1));
            }

            // 2. Nếu không đúng TOTP, thử kiểm tra 10 mã dự phòng
            if (!isTotpValid && !string.IsNullOrEmpty(user.BackupCodesJson))
            {
                var backupCodes = JsonSerializer.Deserialize<List<BackupCodeItem>>(user.BackupCodesJson);
                if (backupCodes != null)
                {
                    var matched = backupCodes.FirstOrDefault(b => !b.IsUsed && 
                        string.Equals(b.Code.Replace("-", ""), cleanCode.Replace("-", ""), StringComparison.OrdinalIgnoreCase));

                    if (matched != null)
                    {
                        matched.IsUsed = true;
                        matched.UsedAt = DateTimeOffset.UtcNow;
                        user.BackupCodesCount = backupCodes.Count(b => !b.IsUsed);
                        user.BackupCodesJson = JsonSerializer.Serialize(backupCodes);
                        isBackupCodeValid = true;
                    }
                }
            }

            if (!isTotpValid && !isBackupCodeValid)
            {
                return ApiResponse<LoginResponseDto>.Fail("Mã xác thực 2 bước hoặc mã dự phòng không chính xác.");
            }

            // 3. Xác thực thành công -> Cấp Access Token & Refresh Token
            var (roleCode, orgName, facilityName) = await GetUserMetadataAsync(user);
            int accessDurationMinutes = _config.GetValue<int>("Jwt:AccessTokenDurationMinutes", 15);
            int refreshDurationDays = _config.GetValue<int>("Jwt:RefreshTokenDurationDays", 7);

            var expiresAt = DateTime.UtcNow.AddMinutes(accessDurationMinutes);
            string accessToken = GenerateJwtToken(user, roleCode, accessDurationMinutes);
            string refreshTokenStr = GenerateRefreshTokenString();

            _context.RefreshTokens.Add(new RefreshToken
            {
                UserId = user.Id,
                Token = refreshTokenStr,
                ExpiresAt = DateTime.UtcNow.AddDays(refreshDurationDays),
                IsRevoked = false
            });

            user.LastLoginAt = DateTimeOffset.UtcNow;
            user.LastActivityAt = DateTimeOffset.UtcNow;
            user.UpdatedAt = DateTimeOffset.UtcNow;
            await _context.SaveChangesAsync();

            string message = isBackupCodeValid 
                ? $"Đăng nhập thành công bằng mã dự phòng. Bạn còn lại {user.BackupCodesCount}/10 mã dự phòng."
                : "Đăng nhập thành công qua xác thực 2 bước.";

            var response = new LoginResponseDto
            {
                AccessToken = accessToken,
                RefreshToken = refreshTokenStr,
                TokenType = "Bearer",
                ExpiresAt = expiresAt,
                Requires2FA = false,
                User = MapUserAuthInfo(user, roleCode, orgName, facilityName)
            };

            return ApiResponse<LoginResponseDto>.Ok(response, message);
        }

        public async Task<ApiResponse<bool>> DisableTwoFactorAsync(Guid userId, DisableTwoFactorDto dto)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted);
            if (user == null)
            {
                return ApiResponse<bool>.Fail("Người dùng không tồn tại.");
            }

            if (!user.TwoFactorEnabled)
            {
                return ApiResponse<bool>.Fail("Tài khoản chưa bật tính năng xác thực 2 bước.");
            }

            // 1. Kiểm tra mật khẩu hiện tại
            if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            {
                return ApiResponse<bool>.Fail("Mật khẩu hiện tại không chính xác.");
            }

            // 2. Kiểm tra mã xác thực (TOTP hoặc Backup code)
            string cleanCode = dto.Code.Trim().Replace(" ", "");
            bool isTotpValid = false;
            if (cleanCode.Length == 6 && int.TryParse(cleanCode, out _) && !string.IsNullOrEmpty(user.TwoFactorSecretKey))
            {
                byte[] secretBytes = Base32Encoding.ToBytes(user.TwoFactorSecretKey);
                var totp = new Totp(secretBytes);
                isTotpValid = totp.VerifyTotp(cleanCode, out _, new VerificationWindow(previous: 1, future: 1));
            }

            bool isBackupCodeValid = false;
            if (!isTotpValid && !string.IsNullOrEmpty(user.BackupCodesJson))
            {
                var backupCodes = JsonSerializer.Deserialize<List<BackupCodeItem>>(user.BackupCodesJson);
                isBackupCodeValid = backupCodes?.Any(b => !b.IsUsed && 
                    string.Equals(b.Code.Replace("-", ""), cleanCode.Replace("-", ""), StringComparison.OrdinalIgnoreCase)) ?? false;
            }

            if (!isTotpValid && !isBackupCodeValid)
            {
                return ApiResponse<bool>.Fail("Mã xác thực 2 bước hoặc mã dự phòng không chính xác.");
            }

            // 3. Tắt 2FA
            user.TwoFactorEnabled = false;
            user.TwoFactorSecretKey = null;
            user.BackupCodesJson = null;
            user.BackupCodesCount = 0;
            user.UpdatedAt = DateTimeOffset.UtcNow;
            await _context.SaveChangesAsync();

            return ApiResponse<bool>.Ok(true, "Đã tắt tính năng xác thực 2 bước (2FA) thành công.");
        }

        public async Task<ApiResponse<List<string>>> RegenerateBackupCodesAsync(Guid userId, RegenerateBackupCodesDto dto)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted);
            if (user == null)
            {
                return ApiResponse<List<string>>.Fail("Người dùng không tồn tại.");
            }

            if (!user.TwoFactorEnabled)
            {
                return ApiResponse<List<string>>.Fail("Tài khoản chưa kích hoạt 2FA.");
            }

            // Kiểm tra mật khẩu
            if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            {
                return ApiResponse<List<string>>.Fail("Mật khẩu xác nhận không chính xác.");
            }

            // Sinh 10 mã mới
            var (backupItems, rawCodes) = Generate10BackupCodes();
            user.BackupCodesJson = JsonSerializer.Serialize(backupItems);
            user.BackupCodesCount = 10;
            user.UpdatedAt = DateTimeOffset.UtcNow;
            await _context.SaveChangesAsync();

            return ApiResponse<List<string>>.Ok(rawCodes, "Đã tạo mới 10 mã dự phòng thành công. Các mã cũ đã bị vô hiệu hóa.");
        }

        public async Task<ApiResponse<int>> GetRemainingBackupCodesCountAsync(Guid userId)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted);
            if (user == null)
            {
                return ApiResponse<int>.Fail("Người dùng không tồn tại.");
            }

            return ApiResponse<int>.Ok(user.BackupCodesCount);
        }

        private (List<BackupCodeItem> items, List<string> rawCodes) Generate10BackupCodes()
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"; // 32 ký tự tránh nhầm lẫn 0/O, 1/I
            var items = new List<BackupCodeItem>();
            var rawCodes = new List<string>();

            for (int i = 0; i < 10; i++)
            {
                var randomBytes = RandomNumberGenerator.GetBytes(8);
                var codeChars = new char[8];
                for (int j = 0; j < 8; j++)
                {
                    codeChars[j] = chars[randomBytes[j] % chars.Length];
                }
                string formattedCode = $"{new string(codeChars, 0, 4)}-{new string(codeChars, 4, 4)}";
                rawCodes.Add(formattedCode);
                items.Add(new BackupCodeItem
                {
                    Code = formattedCode,
                    IsUsed = false,
                    UsedAt = null
                });
            }

            return (items, rawCodes);
        }

        private string FormatSecretKey(string key)
        {
            // Tách cụm 4 ký tự để người dùng dễ đọc và gõ thủ công
            var sb = new StringBuilder();
            for (int i = 0; i < key.Length; i++)
            {
                if (i > 0 && i % 4 == 0) sb.Append(' ');
                sb.Append(key[i]);
            }
            return sb.ToString();
        }
        #endregion

        #endregion
    }
}

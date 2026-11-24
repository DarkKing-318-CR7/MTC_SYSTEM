using System;
using System.ComponentModel.DataAnnotations;

namespace MTC_System.DTOs
{
    public class LoginRequestDto
    {
        [Required(ErrorMessage = "Tên đăng nhập không được để trống")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mật khẩu không được để trống")]
        public string Password { get; set; } = string.Empty;

        public bool RememberMe { get; set; } = false;
    }

    public class LoginResponseDto
    {
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public string TokenType { get; set; } = "Bearer";
        public DateTimeOffset ExpiresAt { get; set; }
        public bool Requires2FA { get; set; } = false;
        public UserAuthInfoDto? User { get; set; }
    }

    public class UserAuthInfoDto
    {
        public Guid UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public Guid? OrganizationId { get; set; }
        public string? OrganizationName { get; set; }
        public Guid? FacilityId { get; set; }
        public string? FacilityName { get; set; }
        public bool IsSuperAdmin { get; set; } = false;
    }

    public class RefreshTokenRequestDto
    {
        [Required(ErrorMessage = "AccessToken là bắt buộc")]
        public string AccessToken { get; set; } = string.Empty;

        [Required(ErrorMessage = "RefreshToken là bắt buộc")]
        public string RefreshToken { get; set; } = string.Empty;
    }

    public class ChangePasswordDto
    {
        [Required(ErrorMessage = "Mật khẩu cũ không được để trống")]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mật khẩu mới không được để trống")]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Xác nhận mật khẩu mới không được để trống")]
        [Compare("NewPassword", ErrorMessage = "Mật khẩu xác nhận không trùng khớp")]
        public string ConfirmNewPassword { get; set; } = string.Empty;
    }

    public class UpdateProfileDto
    {
        [Required(ErrorMessage = "Họ và tên không được để trống")]
        [MaxLength(255)]
        public string FullName { get; set; } = string.Empty;

        [MaxLength(20)]
        public string PhoneNumber { get; set; } = string.Empty;

        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string Email { get; set; } = string.Empty;
    }

    #region 2FA DTOs (Issue #3 / SEC-2FA-LGN-06)
    public class TwoFactorSetupResponseDto
    {
        public string SecretKey { get; set; } = string.Empty;
        public string ManualEntryKey { get; set; } = string.Empty;
        public string QrCodeDataUrl { get; set; } = string.Empty;
        public string OtpAuthUrl { get; set; } = string.Empty;
    }

    public class EnableTwoFactorDto
    {
        [Required(ErrorMessage = "Mã xác thực 6 số là bắt buộc")]
        [RegularExpression(@"^\d{6}$", ErrorMessage = "Mã xác thực phải gồm đúng 6 chữ số")]
        public string Code { get; set; } = string.Empty;
    }

    public class TwoFactorEnableResponseDto
    {
        public bool IsSuccess { get; set; } = true;
        public string Message { get; set; } = string.Empty;
        public List<string> BackupCodes { get; set; } = new();
    }

    public class VerifyTwoFactorLoginDto
    {
        [Required(ErrorMessage = "Mã người dùng (UserId) là bắt buộc")]
        public Guid UserId { get; set; }

        [Required(ErrorMessage = "Mã xác thực 2 bước hoặc mã dự phòng là bắt buộc")]
        public string Code { get; set; } = string.Empty;
    }

    public class DisableTwoFactorDto
    {
        [Required(ErrorMessage = "Mật khẩu hiện tại là bắt buộc")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mã xác thực OTP hoặc mã dự phòng là bắt buộc")]
        public string Code { get; set; } = string.Empty;
    }

    public class RegenerateBackupCodesDto
    {
        [Required(ErrorMessage = "Mật khẩu xác nhận là bắt buộc")]
        public string Password { get; set; } = string.Empty;
    }

    public class BackupCodeItem
    {
        public string Code { get; set; } = string.Empty;
        public bool IsUsed { get; set; } = false;
        public DateTimeOffset? UsedAt { get; set; }
    }
    #endregion
}

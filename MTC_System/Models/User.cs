using System;
using MTC_System.Common;

namespace MTC_System.Models
{
    /// <summary>
    /// [Dev 1] Bảng Users (Tài khoản người dùng)
    /// Kế thừa BaseEntity với OrganizationId? và FacilityId? cho phép null
    /// để lưu được cả System User (toàn hệ thống) lẫn End User (cấp tổ chức / cơ sở).
    /// </summary>
    public class User : BaseEntity
    {
        public Guid? OrganizationId { get; set; } // Null nếu là Quản trị viên nền tảng SaaS (System Admin)
        public Guid? FacilityId { get; set; }     // Null nếu là Quản trị viên cấp Tổ chức hoặc System Admin

        public string Username { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;

        // Xác thực 2 bước (2FA) & Mã dự phòng (Issue #3 / SEC-2FA-LGN-06)
        public bool TwoFactorEnabled { get; set; } = false;
        public string? TwoFactorSecretKey { get; set; }
        public string? BackupCodesJson { get; set; } // Danh sách 10 mã dự phòng
        public int BackupCodesCount { get; set; } = 0;

        // Session & Theo dõi đăng nhập
        public DateTimeOffset? LastLoginAt { get; set; }
        public DateTimeOffset? LastActivityAt { get; set; }
    }
}

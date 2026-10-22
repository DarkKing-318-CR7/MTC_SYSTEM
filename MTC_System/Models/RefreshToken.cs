using System;
using MTC_System.Common;

namespace MTC_System.Models
{
    /// <summary>
    /// [Dev 1] Bảng RefreshTokens (Quản lý phiên đăng nhập JWT)
    /// </summary>
    public class RefreshToken : BaseEntity
    {
        public Guid UserId { get; set; }
        public string Token { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public bool IsRevoked { get; set; } = false;
    }
}

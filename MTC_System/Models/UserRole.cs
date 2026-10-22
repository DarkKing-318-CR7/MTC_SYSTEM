using System;
using MTC_System.Common;

namespace MTC_System.Models
{
    /// <summary>
    /// [Dev 1] Bảng UserRoles (Gán Role cho User)
    /// </summary>
    public class UserRole : BaseEntity
    {
        public Guid UserId { get; set; }
        public Guid RoleId { get; set; }
    }
}

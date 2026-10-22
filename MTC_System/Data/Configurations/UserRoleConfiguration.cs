using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MTC_System.Models;

namespace MTC_System.Data.Configurations
{
    /// <summary>
    /// [Dev 1] Cấu hình bảng UserRoles (Gán vai trò cho người dùng)
    /// </summary>
    public class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
    {
        public void Configure(EntityTypeBuilder<UserRole> builder)
        {
            builder.HasKey(ur => ur.Id);

            // Mỗi cặp UserId + RoleId là duy nhất (không gán trùng)
            builder.HasIndex(ur => new { ur.UserId, ur.RoleId })
                   .IsUnique();

            // Khóa ngoại sang Users: Xóa User thì xóa luôn UserRole của người đó
            builder.HasOne<User>()
                   .WithMany()
                   .HasForeignKey(ur => ur.UserId)
                   .OnDelete(DeleteBehavior.Restrict);

            // Khóa ngoại sang Roles: CẤM xóa Role nếu còn User đang được gán
            builder.HasOne<Role>()
                   .WithMany()
                   .HasForeignKey(ur => ur.RoleId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}

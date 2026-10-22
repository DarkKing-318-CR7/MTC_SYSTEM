using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MTC_System.Models;

namespace MTC_System.Data.Configurations
{
    /// <summary>
    /// [Dev 1] Cấu hình bảng RolePermissions (Ma trận phân quyền Role Matrix)
    /// </summary>
    public class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
    {
        public void Configure(EntityTypeBuilder<RolePermission> builder)
        {
            builder.HasKey(rp => rp.Id);

            // Mỗi cặp RoleId + PermissionId là duy nhất
            builder.HasIndex(rp => new { rp.RoleId, rp.PermissionId })
                   .IsUnique();

            // Khóa ngoại sang Roles: CẤM xóa Role đang có phân quyền
            builder.HasOne<Role>()
                   .WithMany()
                   .HasForeignKey(rp => rp.RoleId)
                   .OnDelete(DeleteBehavior.Restrict);

            // Khóa ngoại sang Permissions: CẤM xóa Permission đang gán cho Role
            builder.HasOne<Permission>()
                   .WithMany()
                   .HasForeignKey(rp => rp.PermissionId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MTC_System.Models;

namespace MTC_System.Data.Configurations
{
    /// <summary>
    /// [Dev 1] Cấu hình bảng Permissions (Danh mục quyền chức năng)
    /// </summary>
    public class PermissionConfiguration : IEntityTypeConfiguration<Permission>
    {
        public void Configure(EntityTypeBuilder<Permission> builder)
        {
            builder.HasKey(p => p.Id);

            builder.Property(p => p.PermissionCode)
                   .IsRequired()
                   .HasMaxLength(100);

            builder.HasIndex(p => p.PermissionCode)
                   .IsUnique();

            builder.Property(p => p.PermissionName)
                   .IsRequired()
                   .HasMaxLength(255);

            builder.Property(p => p.ModuleGroup)
                   .HasMaxLength(100);
        }
    }
}

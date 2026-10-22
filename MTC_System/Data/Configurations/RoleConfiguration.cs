using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MTC_System.Models;

namespace MTC_System.Data.Configurations
{
    /// <summary>
    /// [Dev 1] Cấu hình bảng Roles + Seed dữ liệu 4 vai trò mặc định
    /// </summary>
    public class RoleConfiguration : IEntityTypeConfiguration<Role>
    {
        public void Configure(EntityTypeBuilder<Role> builder)
        {
            builder.HasKey(r => r.Id);

            builder.Property(r => r.RoleCode)
                   .IsRequired()
                   .HasMaxLength(50);

            builder.HasIndex(r => r.RoleCode)
                   .IsUnique();

            builder.Property(r => r.RoleName)
                   .IsRequired()
                   .HasMaxLength(100);

            builder.Property(r => r.Description)
                   .HasMaxLength(500);

            // Khóa ngoại sang Organizations: Cho phép null (System Default Roles = null)
            builder.Property(r => r.OrganizationId)
                   .IsRequired(false);

            builder.HasOne<Organization>()
                   .WithMany()
                   .HasForeignKey(r => r.OrganizationId)
                   .OnDelete(DeleteBehavior.Restrict)
                   .IsRequired(false);

            // SEED: 4 vai trò mặc định của hệ thống (OrganizationId = null)
            builder.HasData(
                new Role
                {
                    Id = Guid.Parse("aaaaaaaa-0001-0000-0000-000000000000"),
                    RoleCode = "ADMINISTRATOR",
                    RoleName = "Quản trị viên",
                    Description = "Toàn quyền quản lý hệ thống",
                    OrganizationId = null,
                    CreatedAt = DateTimeOffset.UtcNow
                },
                new Role
                {
                    Id = Guid.Parse("aaaaaaaa-0002-0000-0000-000000000000"),
                    RoleCode = "CREATOR",
                    RoleName = "Người tạo nội dung",
                    Description = "Tạo và quản lý bản tin, media, playlist",
                    OrganizationId = null,
                    CreatedAt = DateTimeOffset.UtcNow
                },
                new Role
                {
                    Id = Guid.Parse("aaaaaaaa-0003-0000-0000-000000000000"),
                    RoleCode = "IT",
                    RoleName = "Kỹ thuật viên",
                    Description = "Quản lý thiết bị và kết nối kỹ thuật",
                    OrganizationId = null,
                    CreatedAt = DateTimeOffset.UtcNow
                },
                new Role
                {
                    Id = Guid.Parse("aaaaaaaa-0004-0000-0000-000000000000"),
                    RoleCode = "APPROVER",
                    RoleName = "Người phê duyệt",
                    Description = "Phê duyệt bản tin và media trước khi phát sóng",
                    OrganizationId = null,
                    CreatedAt = DateTimeOffset.UtcNow
                }
            );
        }
    }
}

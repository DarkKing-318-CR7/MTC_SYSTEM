using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MTC_System.Models;

namespace MTC_System.Data.Configurations
{
    /// <summary>
    /// [Dev 1] Cấu hình bảng SystemSettings (Tham số cấu hình hệ thống theo Tổ chức)
    /// </summary>
    public class SystemSettingConfiguration : IEntityTypeConfiguration<SystemSetting>
    {
        public void Configure(EntityTypeBuilder<SystemSetting> builder)
        {
            builder.HasKey(s => s.Id);

            builder.Property(s => s.SettingKey)
                   .IsRequired()
                   .HasMaxLength(100);

            // SettingKey là duy nhất trong cùng 1 Tổ chức
            builder.HasIndex(s => new { s.OrganizationId, s.SettingKey })
                   .IsUnique();

            builder.Property(s => s.SettingValue)
                   .IsRequired()
                   .HasMaxLength(1000);

            builder.Property(s => s.Description)
                   .HasMaxLength(500);

            // Liên kết sang Organizations
            builder.Property(s => s.OrganizationId)
                   .IsRequired();

            builder.HasOne<Organization>()
                   .WithMany()
                   .HasForeignKey(s => s.OrganizationId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}

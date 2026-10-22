using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MTC_System.Models;

namespace MTC_System.Data.Configurations
{
    /// <summary>[Dev 4] Cấu hình bảng DeviceVendors + Seed Nhà phân phối Alta Media</summary>
    public class DeviceVendorConfiguration : IEntityTypeConfiguration<DeviceVendor>
    {
        public void Configure(EntityTypeBuilder<DeviceVendor> builder)
        {
            builder.HasKey(v => v.Id);

            builder.Property(v => v.VendorName).IsRequired().HasMaxLength(255);
            builder.Property(v => v.ContactEmail).HasMaxLength(255);
            builder.Property(v => v.ContactPhone).HasMaxLength(20);
            builder.Property(v => v.Website).HasMaxLength(255);
            builder.Property(v => v.Address).HasMaxLength(500);
            builder.Property(v => v.OrganizationId).IsRequired(false);

            // Tên nhà phân phối là duy nhất trong cùng 1 Tổ chức (null = toàn hệ thống)
            builder.HasIndex(v => new { v.OrganizationId, v.VendorName }).IsUnique();

            // Khóa ngoại sang Organizations (Nullable)
            builder.HasOne<Organization>()
                   .WithMany()
                   .HasForeignKey(v => v.OrganizationId)
                   .OnDelete(DeleteBehavior.Restrict)
                   .IsRequired(false);

            // SEED: Nhà phân phối mặc định toàn hệ thống Alta Media
            builder.HasData(
                new DeviceVendor
                {
                    Id = Guid.Parse("dddddddd-0001-0000-0000-000000000000"),
                    VendorName = "Alta Media",
                    ContactEmail = "support@altamedia.vn",
                    ContactPhone = "02822432432",
                    Website = "https://altamedia.vn",
                    Address = "86/33 Âu Cơ, Phường 9, Quận Tân Bình, TP. Hồ Chí Minh",
                    IsDefault = true,
                    CreatedAt = DateTimeOffset.UtcNow
                }
            );
        }
    }
}
